using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using OpenCvSharp;
using Core.Models.Business.OpenCV;

namespace Platform.Windows.Vision
{
    /// <summary>
    /// Template matching over OpenCV.
    ///
    /// Cost is roughly haystack area times template area, so the search area doing the narrowing
    /// matters far more than anything in here. What this does control: match in grayscale (one
    /// channel instead of four), never decode or encode more than once, and pre-scale the template
    /// by the ratio between the area now and the area it was captured in.
    /// </summary>
    public sealed class OpenCvService : IOpenCvService
    {
        // What a normalized correlation can legitimately produce. Anything outside is an artefact
        // of the division, not a measurement - give or take rounding, since a template matching
        // itself comes out a hair over one.
        private const double SCORE_CEILING = 1d;
        private const double SCORE_FLOOR = -1d;
        private const double ROUNDING = 0.001d;

        public TemplateMatchOutcome Match(TemplateMatchRequest request)
        {
            if (request.Haystack.IsEmpty || request.TemplateImage.Length == 0)
                return new TemplateMatchOutcome();

            using Mat haystack = ToGrayMat(request.Haystack);

            // Unchanged, not Grayscale: the editor's eraser leaves transparent pixels, and
            // decoding without alpha turns every erased pixel into a solid black block the screen
            // can never match. Erasing would make a template harder to find, not easier.
            using Mat decoded = Cv2.ImDecode(request.TemplateImage, ImreadModes.Unchanged);

            if (haystack.Empty() || decoded.Empty())
                return new TemplateMatchOutcome();

            using Mat template = ToGrayTemplate(decoded);
            using Mat? mask = ToMask(decoded);

            // Every mode takes a mask, so an erased template is matched with the mode that was
            // asked for. Substituting one that "supports masks" used to silently move CCoeffNormed
            // onto CCorrNormed, which scores 0.95 against blank grey and matched anything.
            TemplateMatchModes mode = ToTemplateMatchModes(request.Mode);

            // One computed scale, one attempt. A sweep around it used to exist and never ran; a
            // wrong ratio is now visible as a low score rather than hidden by guessing near it.
            return MatchAtScale(haystack, template, mask, request, mode, request.ScaleRatio);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static TemplateMatchOutcome MatchAtScale(
            Mat haystack,
            Mat template,
            Mat? mask,
            TemplateMatchRequest request,
            TemplateMatchModes mode,
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
            try
            {
                Mat needle = template;
                Mat? needleMask = mask;

                if (width != template.Width || height != template.Height)
                {
                    scaled = new Mat();
                    // Area averaging keeps edges clean when shrinking, which is the common case.
                    Cv2.Resize(
                        template,
                        scaled,
                        new Size(width, height),
                        interpolation: scale < 1f ? InterpolationFlags.Area : InterpolationFlags.Linear);
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

                if (needleMask == null)
                    Cv2.MatchTemplate(haystack, needle, result, mode);
                else
                    Cv2.MatchTemplate(haystack, needle, result, mode, needleMask);

                return Collect(result, request, mode, width, height, scale);
            }
            finally
            {
                scaled?.Dispose();
                scaledMask?.Dispose();
            }
        }

        /// <summary>
        /// Pull every peak over the threshold. Each accepted hit blanks its own footprint so the
        /// neighbouring pixels of the same match cannot be reported again.
        /// </summary>
        private static TemplateMatchOutcome Collect(
            Mat result,
            TemplateMatchRequest request,
            TemplateMatchModes mode,
            int width,
            int height,
            float scale)
        {
            // The mode actually used, which is not always the one asked for: a masked template
            // may have been moved onto a mask capable mode.
            bool lowerIsBetter = mode == TemplateMatchModes.SqDiffNormed;
            List<TemplateMatchResult> matches = new List<TemplateMatchResult>();
            List<TemplateMatchResult> rejected = new List<TemplateMatchResult>();

            using Mat working = result.Clone();

            // A masked correlation over a flat region divides by zero, and screenshots are full of
            // flat regions: a blank panel, a black transition, a white flash. Nothing over nothing is
            // a NaN, which poisons MinMaxLoc for the whole matrix; anything else over nothing is an
            // infinity, or a number far outside [-1, 1]. None of them is a measurement, so all become
            // the worst score this mode can hold. Clamped to the ceiling instead, an infinity was a
            // perfect match, accepted at any accuracy, in the middle of whatever was blank.
            double worst = lowerIsBetter ? 1d : 0d;
            Cv2.PatchNaNs(working, worst);

            Scalar lowest = new Scalar(SCORE_FLOOR - ROUNDING);
            Scalar highest = new Scalar(SCORE_CEILING + ROUNDING);
            using Mat measured = new Mat();
            using Mat unmeasured = new Mat();
            Cv2.InRange(working, lowest, highest, measured);
            Cv2.BitwiseNot(measured, unmeasured);
            working.SetTo(Scalar.All(worst), unmeasured);

            // What is left is genuine, and only rounding takes it past the bounds.
            Cv2.Min(working, SCORE_CEILING, working);
            Cv2.Max(working, SCORE_FLOOR, working);

            while (matches.Count < request.MaxMatches || rejected.Count < request.RejectedLimit)
            {
                working.MinMaxLoc(out double minValue, out double maxValue, out Point minLocation, out Point maxLocation);

                double score = lowerIsBetter ? 1d - minValue : maxValue;
                Point location = lowerIsBetter ? minLocation : maxLocation;

                // NaN compares false against everything, so without this it is not rejected - it is
                // added as a match with a NaN score. A masked correlation over a flat region produces
                // exactly that.
                if (double.IsNaN(score))
                    break;

                // Below the bar. Keep taking them anyway, up to the limit, so the caller can show
                // where the cut fell - the suppression below stops the same one coming back.
                bool isAccepted = score >= request.AccuracyThreshold;

                if (!isAccepted && rejected.Count >= request.RejectedLimit)
                    break;

                // The screenshot is already full of accepted hits; nothing left to learn from more.
                if (isAccepted && matches.Count >= request.MaxMatches)
                    break;

                (isAccepted ? matches : rejected).Add(new TemplateMatchResult
                {
                    X = location.X,
                    Y = location.Y,
                    Width = width,
                    Height = height,
                    Score = (float)score,
                    Scale = scale,
                });

                // Non maximum suppression: blank the whole footprint, not just the peak.
                Rect footprint = new Rect(
                    Math.Max(0, location.X - width / 2),
                    Math.Max(0, location.Y - height / 2),
                    width,
                    height);

                footprint = footprint.Intersect(new Rect(0, 0, working.Width, working.Height));
                if (footprint.Width <= 0 || footprint.Height <= 0)
                    break;

                working[footprint].SetTo(lowerIsBetter ? Scalar.All(1d) : Scalar.All(0d));
            }

            return new TemplateMatchOutcome
            {
                Matches = matches.OrderByDescending(x => x.Score).ToList(),
                Rejected = rejected.OrderByDescending(x => x.Score).ToList(),
            };
        }

        private static Mat ToGrayMat(RawImage image)
        {
            // Wraps the captured buffer without copying it, then converts once.
            using Mat bgra = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC4, image.Pixels, image.Stride);

            Mat gray = new Mat();
            Cv2.CvtColor(bgra, gray, ColorConversionCodes.BGRA2GRAY);
            return gray;
        }

        /// <summary>Grayscale for matching, whatever the source had.</summary>
        private static Mat ToGrayTemplate(Mat decoded)
        {
            return decoded.Channels() switch
            {
                4 => decoded.CvtColor(ColorConversionCodes.BGRA2GRAY),
                3 => decoded.CvtColor(ColorConversionCodes.BGR2GRAY),
                _ => decoded.Clone(),
            };
        }

        /// <summary>
        /// The alpha channel, which is what the eraser writes. Null when the template is fully
        /// opaque, so an untouched template takes the plain unmasked path it always did.
        /// </summary>
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

        private static TemplateMatchModes ToTemplateMatchModes(TemplateMatchModeEnum mode)
        {
            return mode switch
            {
                TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS => TemplateMatchModes.SqDiffNormed,
                _ => TemplateMatchModes.CCoeffNormed,
            };
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
    }
}
