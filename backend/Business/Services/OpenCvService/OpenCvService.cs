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

        public IReadOnlyList<TemplateMatch> Match(TemplateMatchRequest request)
        {
            if (request.Haystack.IsEmpty || request.TemplateImage.Length == 0)
                return [];

            using Mat haystack = ToGrayMat(request.Haystack);
            using Mat template = Cv2.ImDecode(request.TemplateImage, ImreadModes.Grayscale);

            if (haystack.Empty() || template.Empty())
                return [];

            List<TemplateMatch> matches = MatchAtScale(haystack, template, request, request.ScaleRatio);
            if (matches.Count > 0 || !request.AllowMultiScale)
                return matches;

            // Nothing at the expected size. Sweep around it before giving up.
            foreach (float scale in ScaleSweep(request))
            {
                matches = MatchAtScale(haystack, template, request, scale);
                if (matches.Count > 0)
                    return matches;
            }

            return [];
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static List<TemplateMatch> MatchAtScale(
            Mat haystack,
            Mat template,
            TemplateMatchRequest request,
            float scale)
        {
            int width = (int)MathF.Round(template.Width * scale);
            int height = (int)MathF.Round(template.Height * scale);

            if (width < 2 || height < 2 || width > haystack.Width || height > haystack.Height)
                return [];

            Mat? scaled = null;
            try
            {
                Mat needle = template;

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
                }

                using Mat result = new Mat();
                Cv2.MatchTemplate(haystack, needle, result, ToTemplateMatchModes(request.Mode));

                return Collect(result, request, width, height, scale);
            }
            finally
            {
                scaled?.Dispose();
            }
        }

        /// <summary>
        /// Pull every peak over the threshold. Each accepted hit blanks its own footprint so the
        /// neighbouring pixels of the same match cannot be reported again.
        /// </summary>
        private static List<TemplateMatch> Collect(
            Mat result,
            TemplateMatchRequest request,
            int width,
            int height,
            float scale)
        {
            bool lowerIsBetter = IsLowerBetter(request.Mode);
            List<TemplateMatch> matches = new List<TemplateMatch>();

            using Mat working = result.Clone();

            while (matches.Count < request.MaxMatches)
            {
                working.MinMaxLoc(out double minValue, out double maxValue, out Point minLocation, out Point maxLocation);

                double score = lowerIsBetter ? 1d - minValue : maxValue;
                Point location = lowerIsBetter ? minLocation : maxLocation;

                if (score < request.Threshold)
                    break;

                matches.Add(new TemplateMatch(location.X, location.Y, width, height, (float)score, scale));

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

            return matches.OrderByDescending(x => x.Score).ToList();
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

        private static bool IsLowerBetter(TemplateMatchModeEnum mode) =>
            mode == TemplateMatchModeEnum.SqDiff || mode == TemplateMatchModeEnum.SqDiffNormed;

        private static TemplateMatchModes ToTemplateMatchModes(TemplateMatchModeEnum mode) => mode switch
        {
            TemplateMatchModeEnum.SqDiff => TemplateMatchModes.SqDiff,
            TemplateMatchModeEnum.SqDiffNormed => TemplateMatchModes.SqDiffNormed,
            TemplateMatchModeEnum.CCorr => TemplateMatchModes.CCorr,
            TemplateMatchModeEnum.CCorrNormed => TemplateMatchModes.CCorrNormed,
            TemplateMatchModeEnum.CCoeff => TemplateMatchModes.CCoeff,
            _ => TemplateMatchModes.CCoeffNormed,
        };
    }
}
