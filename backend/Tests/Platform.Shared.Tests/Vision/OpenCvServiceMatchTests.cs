using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.OpenCV;
using OpenCvSharp;
using Platform.Shared.Vision;

namespace Platform.Shared.Tests.Vision
{
    /// <summary>
    /// A template with erased pixels, on a drawn screenshot: seeded noise with a flat black patch,
    /// where the masked correlation divides by zero.
    /// </summary>
    public sealed class OpenCvServiceMatchTests
    {
        private const int WIDTH = 600;
        private const int HEIGHT = 400;
        private const int TEMPLATE = 64;
        private const int ERASED = 20;

        private static readonly Rect FlatPatch = new Rect(300, 100, 240, 220);
        private static readonly Rect TemplateSpot = new Rect(40, 40, TEMPLATE, TEMPLATE);

        private readonly OpenCvService _openCv = new OpenCvService();

        private static Mat Noise(int seed)
        {
            Random random = new Random(seed);
            byte[] pixels = new byte[WIDTH * HEIGHT];
            random.NextBytes(pixels);

            Mat noise = Mat.FromPixelData(HEIGHT, WIDTH, MatType.CV_8UC1, pixels).Clone();
            Cv2.GaussianBlur(noise, noise, new Size(5, 5), 0);
            return noise;
        }

        private static Mat ScreenWithFlatPatch()
        {
            Mat screen = Noise(1);
            screen[FlatPatch].SetTo(Scalar.All(0));
            return screen;
        }

        private static RawImage ToRawImage(Mat gray)
        {
            using Mat bgra = gray.CvtColor(ColorConversionCodes.GRAY2BGRA);

            return new RawImage
            {
                Width = bgra.Width,
                Height = bgra.Height,
                Stride = bgra.Width * 4,
                Pixels = bgra.AsSpan<byte>().ToArray(),
            };
        }

        // The top left corner erased, as the editor's eraser leaves it.
        private static byte[] TemplateWithErasedCorner(Mat source)
        {
            using Mat cut = new Mat(source, TemplateSpot);
            using Mat bgra = cut.CvtColor(ColorConversionCodes.GRAY2BGRA);
            bgra[new Rect(0, 0, ERASED, ERASED)].SetTo(Scalar.All(0));

            return bgra.ImEncode(".png");
        }

        private TemplateMatchOutcome Match(Mat screen, byte[] template)
        {
            return _openCv.Match(new TemplateMatchRequest
            {
                Haystack = ToRawImage(screen),
                TemplateImage = template,
                Mode = TemplateMatchModeEnum.SHAPE,
                AccuracyThreshold = 0.8f,
            });
        }

        // The division over the patch used to come out infinite and be clamped to 1.0: a perfect
        // match, accepted at any accuracy, on a screen without the template on it.
        [Fact]
        public void A_flat_patch_is_no_match_for_a_template_with_erased_pixels()
        {
            using Mat screen = ScreenWithFlatPatch();
            using Mat elsewhere = Noise(2);

            TemplateMatchOutcome outcome = Match(screen, TemplateWithErasedCorner(elsewhere));

            outcome.Matches.ShouldBeEmpty();
            outcome.Rejected.ShouldHaveSingleItem().Score.ShouldBeLessThan(0.8f);
        }

        // A template matching itself comes out a hair over one, which is genuine.
        [Fact]
        public void A_template_with_erased_pixels_still_finds_itself_beside_a_flat_patch()
        {
            using Mat screen = ScreenWithFlatPatch();

            TemplateMatchOutcome outcome = Match(screen, TemplateWithErasedCorner(screen));

            TemplateMatchResult match = outcome.Matches.ShouldHaveSingleItem();
            match.X.ShouldBe(TemplateSpot.X);
            match.Y.ShouldBe(TemplateSpot.Y);
            match.Score.ShouldBeGreaterThan(0.99f);
        }
    }
}
