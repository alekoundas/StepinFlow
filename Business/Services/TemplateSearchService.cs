using Business.Extensions;
using Business.Services.Interfaces;
using Model.Business;
using Model.Enums;
using OpenCvSharp;
using Rectangle = Model.Structs.Rectangle;

namespace Business.Services
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class TemplateSearchService : ITemplateSearchService
    {
        public ISystemService SystemService;
        public TemplateSearchService(ISystemService systemService)
        {
            SystemService = systemService;
        }

        public TemplateMatchingResult SearchForTemplate(byte[] template, byte[] screenshot, TemplateMatchModesEnum? templateMatchModesEnum, bool removeTemplateFromResult)
        {
            OpenCvSharp.TemplateMatchModes matchMode;
            switch (templateMatchModesEnum)
            {
                case TemplateMatchModesEnum.SqDiff:
                    matchMode = TemplateMatchModes.SqDiff;
                    break;
                case TemplateMatchModesEnum.SqDiffNormed:
                    matchMode = TemplateMatchModes.SqDiffNormed;
                    break;
                case TemplateMatchModesEnum.CCorr:
                    matchMode = TemplateMatchModes.CCorr;
                    break;
                case TemplateMatchModesEnum.CCorrNormed:
                    matchMode = TemplateMatchModes.CCorrNormed;
                    break;
                case TemplateMatchModesEnum.CCoeff:
                    matchMode = TemplateMatchModes.CCoeff;
                    break;
                case TemplateMatchModesEnum.CCoeffNormed:
                    matchMode = TemplateMatchModes.CCoeffNormed;
                    break;
                default:
                    matchMode = TemplateMatchModes.CCoeffNormed;
                    break;
            }

            Mat matTemplate = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(template.ToBitmapSource());
            Mat matScreenshot = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(screenshot.ToBitmapSource());
            Mat result;
            //Mat result = matScreenshot.MatchTemplate(matTemplate, matchMode);
            //OpenCvSharp.OpenCVException: '_img.size().height <= _templ.size().height && _img.size().width <= _templ.size().width'
            if (matScreenshot.Height <= matTemplate.Height || matScreenshot.Width <= matTemplate.Width)
            {
                return new TemplateMatchingResult()
                {
                    ResultRectangle = new Rectangle(),
                    Confidence = 0,
                    ResultImage = Array.Empty<byte>(),
                    IsFailed = true,
                    FailiureMessage = "Screenshot is smaller than template image."
                };
            }
            else
                result = matScreenshot.MatchTemplate(matTemplate, matchMode);

            // Execute search.
            result.MinMaxLoc(out double minConfidence,
                             out double maxConfidence,
                             out OpenCvSharp.Point minLoc,
                             out OpenCvSharp.Point maxLoc);



            // Get center possition of template image.
            Rectangle resultRectangle = new Rectangle()
            {
                Top = maxLoc.Y,
                Left = maxLoc.X,
                Right = maxLoc.X + matTemplate.Width,
                Bottom = maxLoc.Y + matTemplate.Height,
            };

            // Convert to %
            decimal minValue = (decimal)minConfidence;
            decimal maxValue = (decimal)maxConfidence;
            decimal r = (decimal)result.At<float>(maxLoc.Y, maxLoc.X);
            decimal percentage = ConvertToPercentage(r, templateMatchModesEnum, minValue, maxValue);

            // Convert to %
            maxConfidence *= 100d;

            // Draws rectangle in result image.
            DrawResultRectangle(percentage, matScreenshot, resultRectangle, removeTemplateFromResult);

            //Save result image to drive for debugging.
            //string resultFilePath = Path.Combine(PathHelper.GetAppDataPath(), "Result.png");
            //matScreenshot.SaveImage(resultFilePath);

            return new TemplateMatchingResult()
            {
                ResultRectangle = resultRectangle,
                Confidence = percentage,
                //ResultImagePath = resultFilePath,
                ResultImage = matScreenshot.ToBytes()
            };
        }


        private decimal ConvertToPercentage(decimal r, TemplateMatchModesEnum? method, decimal minValue = decimal.MinValue, decimal maxValue = decimal.MaxValue)
        {
            decimal percentage;

            switch (method)
            {
                // SqDiff (Min: 0, Max: +\infty)
                case TemplateMatchModesEnum.SqDiff:
                    percentage = maxValue > 0 ? 100 * (1 - r / maxValue) : 100;
                    break;

                // SqDiffNormed (Min: 0, Max: 1)
                case TemplateMatchModesEnum.SqDiffNormed:
                    percentage = 100 * (1 - r);
                    break;

                // CCorr (Min: -\infty, Max: +\infty)
                case TemplateMatchModesEnum.CCorr:
                    percentage = (maxValue - minValue) > 0 ? 100 * (r - minValue) / (maxValue - minValue) : 0;
                    break;

                // CCorrNormed (Min: -1, Max: 1)
                case TemplateMatchModesEnum.CCorrNormed:
                    percentage = 100 * (r + 1) / 2;
                    break;

                // CCoeff (Min: -\infty, Max: +\infty)
                case TemplateMatchModesEnum.CCoeff:
                    percentage = (maxValue - minValue) > 0 ? 100 * (r - minValue) / (maxValue - minValue) : 0;
                    break;

                // CCoeffNormed (Min: -1, Max: 1)
                case TemplateMatchModesEnum.CCoeffNormed:
                    percentage = 100 * (r + 1) / 2;
                    break;

                default:
                    throw new ArgumentException("Invalid template matching method.");
            }

            // Clamp to [0, 100] to handle edge cases or arithmetic errors.
            return Decimal.Max(0, Decimal.Min(100, percentage));
        }


        private static void DrawResultRectangle(decimal confidence, Mat matScreenshot, Rectangle resultRectangle, bool removeTemplateFromResult)
        {
            OpenCvSharp.Point point1 = new OpenCvSharp.Point(resultRectangle.Left, resultRectangle.Top);
            OpenCvSharp.Point point2 = new OpenCvSharp.Point(resultRectangle.Right, resultRectangle.Bottom);

            if (removeTemplateFromResult)
                matScreenshot.Rectangle(point1, point2, new Scalar(0, 0, 255), -1);
            else
                matScreenshot.Rectangle(point1, point2, new Scalar(0, 0, 255), 2);

            //string text = $"Confidence: {Math.Round((float)(maxVal), 2)}";
            string text = Math.Round((float)(confidence), 2).ToString();
            HersheyFonts font = HersheyFonts.HersheyPlain;
            Scalar textColor = new Scalar(255, 0, 0);
            int fontScale = 2;
            int thickness = 4;

            matScreenshot.PutText(text, point1, font, fontScale, textColor, thickness);
        }
    }
}
