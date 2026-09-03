using Core.Enums;
using Core.Models.Business;
using OpenCvSharp;

namespace Business.Services.MatchService
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
        // How many steps either side of the expected scale when the first pass finds nothing.
        private const int MultiScaleSteps = 4;

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

            TemplateMatchOutcome outcome = MatchAtScale(haystack, template, mask, request, mode, request.ScaleRatio);
            if (outcome.Matches.Count > 0 || !request.AllowMultiScale)
                return outcome;

            // Nothing at the expected size. Sweep around it before giving up.
            TemplateMatchOutcome closest = outcome;

            foreach (float scale in ScaleSweep(request))
            {
                TemplateMatchOutcome swept = MatchAtScale(haystack, template, mask, request, mode, scale);
                if (swept.Matches.Count > 0)
                    return swept;

                // The scale that came closest, with its candidates - not the last one tried.
                if (swept.BestScore > closest.BestScore)
                    closest = swept;
            }

            return closest;
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

            if (width < 2 || height < 2 || width > haystack.Width || height > haystack.Height)
                return new TemplateMatchOutcome();

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
            bool lowerIsBetter = mode == TemplateMatchModes.SqDiff || mode == TemplateMatchModes.SqDiffNormed;
            List<TemplateMatchResult> matches = new List<TemplateMatchResult>();
            List<TemplateMatchResult> rejected = new List<TemplateMatchResult>();

            using Mat working = result.Clone();

            // A masked correlation over a flat region is 0/0. Screenshots are full of flat regions,
            // and one NaN poisons MinMaxLoc for the whole matrix - which loses the real match, not
            // just the blank area. Rewrite them as the worst score this mode can hold instead.
            Cv2.PatchNaNs(working, lowerIsBetter ? 1d : 0d);

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
                bool isAccepted = score >= request.Threshold;

                if (!isAccepted && rejected.Count >= request.RejectedLimit)
                    break;

                // The frame is already full of accepted hits; nothing left to learn from more.
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

        private static IEnumerable<float> ScaleSweep(TemplateMatchRequest request)
        {
            float step = request.ScaleTolerance / MultiScaleSteps;

            for (int i = 1; i <= MultiScaleSteps; i++)
            {
                yield return request.ScaleRatio + step * i;
                yield return request.ScaleRatio - step * i;
            }
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
        private static Mat ToGrayTemplate(Mat decoded) => decoded.Channels() switch
        {
            4 => decoded.CvtColor(ColorConversionCodes.BGRA2GRAY),
            3 => decoded.CvtColor(ColorConversionCodes.BGR2GRAY),
            _ => decoded.Clone(),
        };

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

        private static TemplateMatchModes ToTemplateMatchModes(TemplateMatchModeEnum mode) => mode switch
        {
            TemplateMatchModeEnum.SqDiff => TemplateMatchModes.SqDiff,
            TemplateMatchModeEnum.SqDiffNormed => TemplateMatchModes.SqDiffNormed,
            TemplateMatchModeEnum.CCorr => TemplateMatchModes.CCorr,
            TemplateMatchModeEnum.CCorrNormed => TemplateMatchModes.CCorrNormed,
            TemplateMatchModeEnum.CCoeff => TemplateMatchModes.CCoeff,
            _ => TemplateMatchModes.CCoeffNormed,
        };    }
}
