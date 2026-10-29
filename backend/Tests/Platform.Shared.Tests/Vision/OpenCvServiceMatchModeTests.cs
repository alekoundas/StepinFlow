using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.OpenCV;
using OpenCvSharp;
using Platform.Shared.Vision;

namespace Platform.Shared.Tests.Vision
{
    /// <summary>
    /// The match-mode table in PROJECT.md §8, on screenshots headless Edge rendered at 100%: a letter
    /// in Segoe UI on white, and a PrimeReact Lara button enabled and disabled. Each template is cut
    /// from its screenshot with 4 pixels around it, the loose one with 10.
    /// </summary>
    public sealed class OpenCvServiceMatchModeTests
    {
        private const float SHAPE_ACCURACY = 0.8f;
        private const float SHAPE_AND_BRIGHTNESS_ACCURACY = 0.95f;

        // The table's last digit.
        private const double TABLE_PRECISION = 0.001;

        private readonly OpenCvService _openCv = new OpenCvService();

        private static byte[] Asset(string folder, string name)
        {
            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", folder, name + ".png"));
        }

        private static RawImage Screenshot(string name)
        {
            using Mat decoded = Cv2.ImDecode(Asset("Screenshots", name), ImreadModes.Color);
            using Mat bgra = decoded.CvtColor(ColorConversionCodes.BGR2BGRA);

            return new RawImage
            {
                Width = bgra.Width,
                Height = bgra.Height,
                Stride = bgra.Width * 4,
                Pixels = bgra.AsSpan<byte>().ToArray(),
            };
        }

        private TemplateMatchOutcome Match(string template, string screenshot, TemplateMatchModeEnum mode, float accuracy)
        {
            return _openCv.Match(new TemplateMatchRequest
            {
                Haystack = Screenshot(screenshot),
                TemplateImage = Asset("Templates", template),
                Mode = mode,
                AccuracyThreshold = accuracy,
                MaxMatches = 1,
            });
        }

        [Theory]
        [InlineData("letter-a", "letter-a", TemplateMatchModeEnum.SHAPE, 1.000)]
        [InlineData("letter-a", "letter-a", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 1.000)]
        [InlineData("button", "button-enabled", TemplateMatchModeEnum.SHAPE, 1.000)]
        [InlineData("button", "button-enabled", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 1.000)]
        [InlineData("letter-a", "blank", TemplateMatchModeEnum.SHAPE, 0.000)]
        [InlineData("letter-a", "blank", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.866)]
        [InlineData("letter-a", "letter-b", TemplateMatchModeEnum.SHAPE, 0.523)]
        [InlineData("letter-a", "letter-b", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.878)]
        [InlineData("button", "button-disabled", TemplateMatchModeEnum.SHAPE, 1.000)]
        [InlineData("button", "button-disabled", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.937)]
        [InlineData("button-loose", "button-disabled", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, 0.964)]
        public void Each_case_scores_what_the_table_says(string template, string screenshot, TemplateMatchModeEnum mode, double score)
        {
            TemplateMatchOutcome outcome = Match(template, screenshot, mode, 1f);

            double best = outcome.Matches.Concat(outcome.Rejected).Max(x => x.Score);
            best.ShouldBe(score, TABLE_PRECISION);
        }

        // A disabled button is the enabled one paler, and shape forgives brightness.
        [Fact]
        public void Shape_finds_the_enabled_button_where_only_the_disabled_one_is()
        {
            Match("button", "button-disabled", TemplateMatchModeEnum.SHAPE, SHAPE_ACCURACY).Matches.ShouldHaveSingleItem();
        }

        [Fact]
        public void Shape_and_brightness_at_its_own_accuracy_finds_none_of_the_wrong_cases()
        {
            TemplateMatchModeEnum mode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS;

            Match("letter-a", "blank", mode, SHAPE_AND_BRIGHTNESS_ACCURACY).Matches.ShouldBeEmpty();
            Match("letter-a", "letter-b", mode, SHAPE_AND_BRIGHTNESS_ACCURACY).Matches.ShouldBeEmpty();
            Match("button", "button-disabled", mode, SHAPE_AND_BRIGHTNESS_ACCURACY).Matches.ShouldBeEmpty();
        }

        // Most of a crop is background, and background always agrees: why its bar is 0.95.
        [Fact]
        public void Shape_and_brightness_at_shape_s_accuracy_finds_an_A_on_a_blank_screen()
        {
            Match("letter-a", "blank", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, SHAPE_ACCURACY).Matches.ShouldHaveSingleItem();
        }

        // A known limit: more background lifts the score past the bar.
        [Fact]
        public void A_button_template_cut_loose_is_found_disabled_even_by_shape_and_brightness()
        {
            Match("button-loose", "button-disabled", TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS, SHAPE_AND_BRIGHTNESS_ACCURACY).Matches.ShouldHaveSingleItem();
        }
    }
}
