using Core.Ports;
using Core.Enums;
using OpenCvSharp;
using Core.Models.Business.OpenCV;

namespace Platform.Shared.Vision
{
    /// <summary>
    /// Template matching over OpenCV.
    ///
    /// Cost is roughly haystack area times template area, so the search area doing the narrowing
    /// matters far more than anything in here. What this does control: match in grayscale (one
    /// channel instead of four), never decode or encode more than once, and pre-scale the template
    /// by the ratio between the area now and the area it was captured in. Shape and colour looks at
    /// colour only where the shape has already passed.
    /// </summary>
    public sealed class OpenCvService : IOpenCvService
    {
        // What a normalized correlation can legitimately produce. Anything outside is an artefact
        // of the division, not a measurement - give or take rounding, since a template matching
        // itself comes out a hair over one.
        private const double SCORE_CEILING = 1d;
        private const double SCORE_FLOOR = -1d;
        private const double ROUNDING = 0.001d;

        // Shape and colour stops after refusing this many right shapes, so a template that matches
        // everywhere cannot keep it searching.
        private const int REFUSED_LIMIT = 20;

        // About what a screenshot at another scale is softened by, so that is not read as contrast.
        private const double CONTRAST_BLUR_SIGMA = 1.5d;

        // A template whose lightness hardly varies has no contrast to compare.
        private const double MINIMUM_SPREAD = 1d;

        private const double NO_WEIGHT = 1e-6d;

        public TemplateMatchOutcome Match(TemplateMatchRequest request)
        {
            if (request.Haystack.IsEmpty || request.TemplateImage.Length == 0)
                return new TemplateMatchOutcome();

            // Wraps the captured buffer without copying it, then converts once.
            using Mat screen = Mat.FromPixelData(request.Haystack.Height, request.Haystack.Width, MatType.CV_8UC4, request.Haystack.Pixels, request.Haystack.Stride);
            using Mat haystack = screen.CvtColor(ColorConversionCodes.BGRA2GRAY);

            // Unchanged, not Grayscale: the editor's eraser leaves transparent pixels, and
            // decoding without alpha turns every erased pixel into a solid black block the screen
            // can never match. Erasing would make a template harder to find, not easier.
            using Mat decoded = Cv2.ImDecode(request.TemplateImage, ImreadModes.Unchanged);

            if (haystack.Empty() || decoded.Empty())
                return new TemplateMatchOutcome();

            using Mat template = ToGrayTemplate(decoded);
            using Mat? mask = ToMask(decoded);

            // One computed scale, one attempt. A sweep around it used to exist and never ran; a
            // wrong ratio is now visible as a low score rather than hidden by guessing near it.
            return MatchAtScale(screen, haystack, decoded, template, mask, request, request.ScaleRatio);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static TemplateMatchOutcome MatchAtScale(
            Mat screen,
            Mat haystack,
            Mat decoded,
            Mat template,
            Mat? mask,
            TemplateMatchRequest request,
            float scale)
        {
            int width = (int)MathF.Round(template.Width * scale);
            int height = (int)MathF.Round(template.Height * scale);

            // Not an empty result: a ratio that makes the template bigger than the screenshot, or
            // nothing at all, means the scaling key is wrong, and "not found" would hide it.
            if (width < 2 || height < 2 || width > haystack.Width || height > haystack.Height)
            {
                return new TemplateMatchOutcome
                {
                    Error = FormattableString.Invariant($"scaled by {scale:0.00} it is {width}x{height}, and the area it is searched in is {haystack.Width}x{haystack.Height}."),
                };
            }

            Mat? scaled = null;
            Mat? scaledMask = null;
            ColourTemplate? colour = null;
            try
            {
                Mat needle = template;
                Mat? needleMask = mask;

                if (width != template.Width || height != template.Height)
                {
                    scaled = Resize(template, width, height, scale);
                    needle = scaled;

                    if (mask != null)
                    {
                        // Nearest, not Area: OpenCV treats any non zero as fully included, so
                        // blending the edges would only widen the kept region.
                        scaledMask = new Mat();
                        Cv2.Resize(mask, scaledMask, new Size(width, height), interpolation: InterpolationFlags.Nearest);
                        needleMask = scaledMask;
                    }
                }

                using Mat result = new Mat();

                // CCoeffNormed in both modes, masked as it is. A "mask capable" substitute used to
                // move it onto CCorrNormed, which scores 0.95 against blank grey and matched anything.
                if (needleMask == null)
                    Cv2.MatchTemplate(haystack, needle, result, TemplateMatchModes.CCoeffNormed);
                else
                    Cv2.MatchTemplate(haystack, needle, result, TemplateMatchModes.CCoeffNormed, needleMask);

                if (request.Mode == TemplateMatchModeEnum.SHAPE_AND_COLOUR)
                    colour = MeasureColour(decoded, needleMask, width, height, scale);

                return Collect(result, request, width, height, scale, screen, colour);
            }
            finally
            {
                scaled?.Dispose();
                scaledMask?.Dispose();
                colour?.Dispose();
            }
        }

        // Every peak over the threshold, best first. Each one blanks its own footprint so the
        // neighbouring pixels of the same match cannot be reported again. With a colour template a
        // peak over the threshold must also pass its colour and contrast, and one that does not is
        // refused for the next one down, so a greyed copy scoring higher cannot hide the live one.
        private static TemplateMatchOutcome Collect(
            Mat result,
            TemplateMatchRequest request,
            int width,
            int height,
            float scale,
            Mat screen,
            ColourTemplate? colour)
        {
            List<TemplateMatchResult> matches = new List<TemplateMatchResult>();
            List<TemplateMatchResult> rejected = new List<TemplateMatchResult>();
            List<TemplateMatchResult> refused = new List<TemplateMatchResult>();

            using Mat working = result.Clone();

            // A masked correlation over a flat region divides by zero, and screenshots are full of
            // flat regions: a blank panel, a black transition, a white flash. Nothing over nothing is
            // a NaN, which poisons MinMaxLoc for the whole matrix; anything else over nothing is an
            // infinity, or a number far outside [-1, 1]. None of them is a measurement, so all become
            // 0, nothing alike. Clamped to the ceiling instead, an infinity was a perfect match,
            // accepted at any accuracy, in the middle of whatever was blank.
            Cv2.PatchNaNs(working, 0d);

            Scalar lowest = new Scalar(SCORE_FLOOR - ROUNDING);
            Scalar highest = new Scalar(SCORE_CEILING + ROUNDING);
            using Mat measured = new Mat();
            using Mat unmeasured = new Mat();
            Cv2.InRange(working, lowest, highest, measured);
            Cv2.BitwiseNot(measured, unmeasured);
            working.SetTo(Scalar.All(0d), unmeasured);

            // What is left is genuine, and only rounding takes it past the bounds.
            Cv2.Min(working, SCORE_CEILING, working);
            Cv2.Max(working, SCORE_FLOOR, working);

            while (matches.Count < request.MaxMatches || rejected.Count < request.RejectedLimit)
            {
                working.MinMaxLoc(out double _, out double score, out Point _, out Point location);

                // NaN compares false against everything, so without this it is not rejected - it is
                // added as a match with a NaN score. A masked correlation over a flat region produces
                // exactly that.
                if (double.IsNaN(score))
                    break;

                // Below the bar. Keep taking them anyway, up to the limit, so the caller can show
                // where the cut fell - the suppression below stops the same one coming back.
                bool isOverBar = score >= request.AccuracyThreshold;

                if (!isOverBar && rejected.Count >= request.RejectedLimit)
                    break;

                // The screenshot is already full of accepted hits; nothing left to learn from more.
                if (isOverBar && matches.Count >= request.MaxMatches)
                    break;

                if (isOverBar && refused.Count >= REFUSED_LIMIT)
                    break;

                TemplateMatchResult candidate = new TemplateMatchResult
                {
                    X = location.X,
                    Y = location.Y,
                    Width = width,
                    Height = height,
                    Score = (float)score,
                    Scale = scale,
                };

                if (!isOverBar)
                    rejected.Add(candidate);
                else if (colour == null || IsSameColour(screen, colour, location, candidate))
                    matches.Add(candidate);
                else
                    refused.Add(candidate);

                // Non maximum suppression: blank the whole footprint, not just the peak.
                Rect footprint = new Rect(
                    Math.Max(0, location.X - width / 2),
                    Math.Max(0, location.Y - height / 2),
                    width,
                    height);

                footprint = footprint.Intersect(new Rect(0, 0, working.Width, working.Height));
                if (footprint.Width <= 0 || footprint.Height <= 0)
                    break;

                using Mat blanked = working[footprint];
                blanked.SetTo(Scalar.All(0d));
            }

            return new TemplateMatchOutcome
            {
                Matches = matches.OrderByDescending(x => x.Score).ToList(),
                Rejected = rejected.OrderByDescending(x => x.Score).ToList(),
                Refused = refused.OrderByDescending(x => x.Score).ToList(),
            };
        }

        // The candidate's average colour, lightness left out, and how widely its lightness varies,
        // each against the template's. Both are written on the candidate, refused or not.
        private static bool IsSameColour(Mat screen, ColourTemplate template, Point at, TemplateMatchResult candidate)
        {
            using Mat patch = screen[new Rect(at.X, at.Y, template.Width, template.Height)];
            using Mat bgr = patch.CvtColor(ColorConversionCodes.BGRA2BGR);
            using Mat lab = ToLab(bgr);

            Scalar mean = MeanOf(lab, template.Mask);
            double aApart = template.A - mean.Val1;
            double bApart = template.B - mean.Val2;
            double colour = Math.Sqrt(aApart * aApart + bApart * bApart);

            double contrast = 1d;
            if (template.Spread >= MINIMUM_SPREAD)
                contrast = Spread(lab, template.Mask, template.Weight, template.WeightBlurred) / template.Spread;

            candidate.Colour = (float)colour;
            candidate.ColourLimit = (float)template.ColourLimit;
            candidate.Contrast = (float)contrast;

            if (colour > template.ColourLimit)
                candidate.RefusedBy = MatchRefusalEnum.COLOUR;
            else if (contrast < ColourCheckLimits.CONTRAST_MIN || contrast > ColourCheckLimits.CONTRAST_MAX)
                candidate.RefusedBy = MatchRefusalEnum.CONTRAST;

            return candidate.RefusedBy == null;
        }

        // The template in colour at the size it is matched at, measured once for all its candidates.
        private static ColourTemplate MeasureColour(Mat decoded, Mat? mask, int width, int height, float scale)
        {
            using Mat bgr = ToBgr(decoded);
            using Mat sized = Resize(bgr, width, height, scale);
            using Mat lab = ToLab(sized);

            Mat? weight = null;
            Mat? weightBlurred = null;

            if (mask != null)
            {
                // Kept or not, as the match reads the mask, so the blur is fed by kept pixels only.
                using Mat kept = new Mat();
                Cv2.Threshold(mask, kept, 0, 255, ThresholdTypes.Binary);

                weight = new Mat();
                kept.ConvertTo(weight, MatType.CV_32F, 1d / 255d);

                weightBlurred = new Mat();
                Cv2.GaussianBlur(weight, weightBlurred, new Size(0, 0), CONTRAST_BLUR_SIGMA);
                Cv2.Max(weightBlurred, NO_WEIGHT, weightBlurred);
            }

            Scalar mean = MeanOf(lab, mask);
            double strength = Math.Sqrt(mean.Val1 * mean.Val1 + mean.Val2 * mean.Val2);
            double limit = Math.Max(ColourCheckLimits.COLOUR_FLOOR, ColourCheckLimits.COLOUR_SHARE * strength);
            double spread = Spread(lab, mask, weight, weightBlurred);

            return new ColourTemplate(width, height, mask, weight, weightBlurred, mean.Val1, mean.Val2, limit, spread);
        }

        // How widely the lightness varies over the kept pixels, after a light blur only they feed.
        private static double Spread(Mat lab, Mat? mask, Mat? weight, Mat? weightBlurred)
        {
            using Mat lightness = lab.ExtractChannel(0);
            using Mat blurred = new Mat();

            if (weight == null || weightBlurred == null)
            {
                Cv2.GaussianBlur(lightness, blurred, new Size(0, 0), CONTRAST_BLUR_SIGMA);
            }
            else
            {
                using Mat weighted = new Mat();
                Cv2.Multiply(lightness, weight, weighted);
                Cv2.GaussianBlur(weighted, blurred, new Size(0, 0), CONTRAST_BLUR_SIGMA);
                Cv2.Divide(blurred, weightBlurred, blurred);
            }

            Scalar deviation;
            if (mask == null)
                Cv2.MeanStdDev(blurred, out Scalar _, out deviation);
            else
                Cv2.MeanStdDev(blurred, out Scalar _, out deviation, mask);

            return deviation.Val0;
        }

        private static Scalar MeanOf(Mat image, Mat? mask)
        {
            if (mask == null)
                return Cv2.Mean(image);

            return Cv2.Mean(image, mask);
        }

        // CIE Lab with the sRGB curve: lightness 0 to 100, a and b either side of 0.
        private static Mat ToLab(Mat bgr)
        {
            using Mat unit = new Mat();
            bgr.ConvertTo(unit, MatType.CV_32FC3, 1d / 255d);

            Mat lab = new Mat();
            Cv2.CvtColor(unit, lab, ColorConversionCodes.BGR2Lab);
            return lab;
        }

        // Area averaging keeps edges clean when shrinking, which is the common case.
        private static Mat Resize(Mat image, int width, int height, float scale)
        {
            InterpolationFlags interpolation = InterpolationFlags.Linear;
            if (scale < 1f)
                interpolation = InterpolationFlags.Area;

            Mat resized = new Mat();
            Cv2.Resize(image, resized, new Size(width, height), interpolation: interpolation);
            return resized;
        }

        // Grayscale for matching, whatever the source had.
        private static Mat ToGrayTemplate(Mat decoded)
        {
            return decoded.Channels() switch
            {
                4 => decoded.CvtColor(ColorConversionCodes.BGRA2GRAY),
                3 => decoded.CvtColor(ColorConversionCodes.BGR2GRAY),
                _ => decoded.Clone(),
            };
        }

        private static Mat ToBgr(Mat decoded)
        {
            return decoded.Channels() switch
            {
                4 => decoded.CvtColor(ColorConversionCodes.BGRA2BGR),
                3 => decoded.Clone(),
                _ => decoded.CvtColor(ColorConversionCodes.GRAY2BGR),
            };
        }

        // The alpha channel, which is what the eraser writes. Null when the template is fully
        // opaque, so an untouched template takes the plain unmasked path it always did.
        private static Mat? ToMask(Mat decoded)
        {
            if (decoded.Channels() != 4)
                return null;

            Mat alpha = decoded.ExtractChannel(3);
            alpha.MinMaxLoc(out double minValue, out _);

            if (minValue >= 255d)
            {
                alpha.Dispose();
                return null;
            }

            return alpha;
        }


        // ================================================================
        // Grouping clicked elements that are the same picture
        // ================================================================

        // Clear of the noise floor rather than near it. Measured against real templates, an
        // unrelated pair scores under 0.45 and a genuine match sits well above 0.8.
        private const double SAME_PICTURE = 0.8;

        // Flat colour matches every other flat colour. An element this featureless - a blank field,
        // a plain panel - says nothing about what was clicked.
        private const double MINIMUM_DETAIL = 12d;

        public IReadOnlyList<IReadOnlyList<int>> GroupSameElements(IReadOnlyList<ElementPicture> elements)
        {
            List<DecodedElement> usable = new List<DecodedElement>();

            try
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    Mat element = Cv2.ImDecode(elements[i].Element, ImreadModes.Color);
                    Mat around = Cv2.ImDecode(elements[i].Around, ImreadModes.Color);
                    DecodedElement decoded = new DecodedElement(i, element, around);

                    if (decoded.Element.Empty() || decoded.Around.Empty() || !HasDetail(decoded.Element))
                    {
                        decoded.Dispose();
                        continue;
                    }

                    usable.Add(decoded);
                }

                // Greedy: the first element of a group is what the rest are compared against, so a
                // group never drifts away from what started it.
                List<List<int>> groups = new List<List<int>>();
                List<DecodedElement> leaders = new List<DecodedElement>();

                foreach (DecodedElement element in usable)
                {
                    int found = leaders.FindIndex(x => IsSamePicture(x, element));

                    if (found >= 0)
                    {
                        groups[found].Add(element.Index);
                    }
                    else
                    {
                        groups.Add([element.Index]);
                        leaders.Add(element);
                    }
                }

                return groups.Where(x => x.Count >= 2).ToList();
            }
            finally
            {
                foreach (DecodedElement element in usable)
                    element.Dispose();
            }
        }

        // Both ways, so a small element is never "found" inside a bigger one it is only part of.
        private static bool IsSamePicture(DecodedElement first, DecodedElement second)
        {
            return IsFoundIn(first.Element, second.Around) && IsFoundIn(second.Element, first.Around);
        }

        private static bool IsFoundIn(Mat element, Mat around)
        {
            if (element.Width > around.Width || element.Height > around.Height)
                return false;

            using Mat result = new Mat();
            Cv2.MatchTemplate(around, element, result, TemplateMatchModes.CCoeffNormed);
            Cv2.PatchNaNs(result, 0d);

            result.MinMaxLoc(out double _, out double best);

            return best >= SAME_PICTURE;
        }

        private static bool HasDetail(Mat image)
        {
            using Mat grey = image.CvtColor(ColorConversionCodes.BGR2GRAY);
            Cv2.MeanStdDev(grey, out Scalar _, out Scalar deviation);

            return deviation.Val0 >= MINIMUM_DETAIL;
        }


        // ================================================================
        // Re-encoding
        // ================================================================

        public byte[]? FlattenErasedPixels(byte[] png)
        {
            try
            {
                using Mat decoded = Cv2.ImDecode(png, ImreadModes.Unchanged);
                if (decoded.Empty())
                    return null;

                if (decoded.Channels() != 4)
                    return decoded.ImEncode(".png");

                using Mat colour = new Mat();
                Cv2.CvtColor(decoded, colour, ColorConversionCodes.BGRA2BGR);

                // Only the fully erased pixels. A threshold rather than an inverted alpha, because
                // inverting turns a half transparent pixel fully white, and those were part of the
                // search - at less weight, but they were there.
                using Mat alpha = decoded.ExtractChannel(3);
                using Mat erased = new Mat();
                Cv2.Threshold(alpha, erased, 0, 255, ThresholdTypes.BinaryInv);

                colour.SetTo(Scalar.All(255), erased);

                return colour.ImEncode(".png");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte[] Downscale(byte[] jpeg, int maxEdge, int quality)
        {
            using Mat decoded = Cv2.ImDecode(jpeg, ImreadModes.Color);
            if (decoded.Empty())
                return jpeg;

            int longest = Math.Max(decoded.Width, decoded.Height);
            if (longest <= maxEdge)
                return jpeg;

            double scale = (double)maxEdge / longest;

            using Mat resized = new Mat();
            Cv2.Resize(decoded, resized, new Size(0, 0), scale, scale, InterpolationFlags.Area);

            return resized.ImEncode(".jpg", [(int)ImwriteFlags.JpegQuality, quality]);
        }

        public byte[]? Thumbnail(byte[] png, int maxEdge)
        {
            try
            {
                using Mat decoded = Cv2.ImDecode(png, ImreadModes.Unchanged);
                if (decoded.Empty())
                    return null;

                int longest = Math.Max(decoded.Width, decoded.Height);
                if (longest <= maxEdge)
                    return png;

                double scale = (double)maxEdge / longest;

                using Mat resized = new Mat();
                Cv2.Resize(decoded, resized, new Size(0, 0), scale, scale, InterpolationFlags.Area);

                return resized.ImEncode(".png");
            }
            catch (Exception)
            {
                return null;
            }
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record DecodedElement(int Index, Mat Element, Mat Around) : IDisposable
        {
            public void Dispose()
            {
                Element.Dispose();
                Around.Dispose();
            }
        }

        // A template measured for the colour check, at the size it is matched at. The mask is the
        // match's, so only what the check made for itself is let go here.
        private sealed record ColourTemplate(
            int Width,
            int Height,
            Mat? Mask,
            Mat? Weight,
            Mat? WeightBlurred,
            double A,
            double B,
            double ColourLimit,
            double Spread) : IDisposable
        {
            public void Dispose()
            {
                Weight?.Dispose();
                WeightBlurred?.Dispose();
            }
        }
    }
}
