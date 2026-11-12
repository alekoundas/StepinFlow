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
        private const float ACCURACY = 0.8f;

        // The table's last digit for a score, one decimal for a colour, two for a contrast.
        private const double TABLE_PRECISION = 0.001;
        private const double COLOUR_PRECISION = 0.1;
        private const double CONTRAST_PRECISION = 0.01;

        private readonly OpenCvService _openCv = new OpenCvService();


        // ================================================================
        // The table
        // ================================================================

        // Shape and colour adds its checks on top of the same shape score, never instead of it.
        [Theory]
        [InlineData("letter-a", "letter-a", 1.000)]
        [InlineData("button", "button-enabled", 1.000)]
        [InlineData("letter-a", "blank", 0.000)]
        [InlineData("letter-a", "letter-b", 0.523)]
        [InlineData("button", "button-disabled", 1.000)]
        [InlineData("button-loose", "button-disabled", 1.000)]
        public void Each_case_scores_the_same_shape_in_both_modes(string template, string screenshot, double score)
        {
            foreach (TemplateMatchModeEnum mode in Enum.GetValues<TemplateMatchModeEnum>())
            {
                TemplateMatchOutcome outcome = Match(template, Screenshot(screenshot), mode, accuracy: 1f);

                double best = outcome.Matches.Concat(outcome.Refused).Concat(outcome.Rejected).Max(x => x.Score);
                best.ShouldBe(score, TABLE_PRECISION, mode.ToString());
            }
        }

        // A disabled button is the enabled one paler, and shape forgives that.
        [Fact]
        public void Shape_finds_the_enabled_button_where_only_the_disabled_one_is()
        {
            Match("button", Screenshot("button-disabled"), TemplateMatchModeEnum.SHAPE).Matches.ShouldHaveSingleItem();
        }

        [Fact]
        public void Shape_never_looks_at_colour()
        {
            TemplateMatchResult hit = Match("button", Screenshot("button-disabled"), TemplateMatchModeEnum.SHAPE).Matches.Single();

            hit.Colour.ShouldBeNull();
            hit.Contrast.ShouldBeNull();
            hit.RefusedBy.ShouldBeNull();
        }

        [Theory]
        [InlineData("letter-a", "letter-a")]
        [InlineData("button", "button-enabled")]
        [InlineData("button-loose", "button-enabled")]
        public void Shape_and_colour_finds_the_right_template_with_nothing_apart(string template, string screenshot)
        {
            TemplateMatchResult hit = Match(template, Screenshot(screenshot), TemplateMatchModeEnum.SHAPE_AND_COLOUR).Matches.ShouldHaveSingleItem();

            hit.Colour!.Value.ShouldBe(0f, COLOUR_PRECISION);
            hit.Contrast!.Value.ShouldBe(1f, CONTRAST_PRECISION);
            hit.RefusedBy.ShouldBeNull();
        }

        // Loose or tight, the change is a share of the template's own colour, so the crop does not
        // decide it the way background decided the old brightness mode.
        [Theory]
        [InlineData("button", 25.0, 16.2)]
        [InlineData("button-loose", 18.0, 11.7)]
        public void Shape_and_colour_refuses_the_disabled_button_for_its_colour_however_it_is_cut(string template, double colour, double limit)
        {
            TemplateMatchOutcome outcome = Match(template, Screenshot("button-disabled"), TemplateMatchModeEnum.SHAPE_AND_COLOUR);

            outcome.Matches.ShouldBeEmpty();
            TemplateMatchResult refused = outcome.Refused.ShouldHaveSingleItem();
            refused.RefusedBy.ShouldBe(MatchRefusalEnum.COLOUR);
            refused.Score.ShouldBe(1f, TABLE_PRECISION);
            refused.Colour!.Value.ShouldBe((float)colour, COLOUR_PRECISION);
            refused.ColourLimit!.Value.ShouldBe((float)limit, COLOUR_PRECISION);
            refused.Contrast!.Value.ShouldBe(0.61f, CONTRAST_PRECISION);
        }

        [Theory]
        [InlineData("blank")]
        [InlineData("letter-b")]
        public void Shape_and_colour_has_nothing_to_refuse_where_the_shape_is_not_there(string screenshot)
        {
            TemplateMatchOutcome outcome = Match("letter-a", Screenshot(screenshot), TemplateMatchModeEnum.SHAPE_AND_COLOUR);

            outcome.Matches.ShouldBeEmpty();
            outcome.Refused.ShouldBeEmpty();
            outcome.Rejected.ShouldHaveSingleItem();
        }


        // ================================================================
        // Contrast
        // ================================================================

        // A letter has no colour to lose, so only its contrast can tell a faded one.
        [Fact]
        public void A_letter_faded_to_70_percent_is_refused_for_its_contrast()
        {
            using Mat faded = Faded("letter-a", 0.7);

            TemplateMatchResult refused = Match("letter-a", Raw(faded), TemplateMatchModeEnum.SHAPE_AND_COLOUR).Refused.ShouldHaveSingleItem();

            refused.RefusedBy.ShouldBe(MatchRefusalEnum.CONTRAST);
            refused.Contrast!.Value.ShouldBe(0.675f, CONTRAST_PRECISION);
            refused.Colour!.Value.ShouldBeLessThan(refused.ColourLimit!.Value);
        }

        [Fact]
        public void A_letter_faded_to_90_percent_is_still_found()
        {
            using Mat faded = Faded("letter-a", 0.9);

            TemplateMatchResult hit = Match("letter-a", Raw(faded), TemplateMatchModeEnum.SHAPE_AND_COLOUR).Matches.ShouldHaveSingleItem();

            hit.Contrast!.Value.ShouldBe(0.889f, CONTRAST_PRECISION);
        }

        // What a screenshot at another scale looks like: softer, which a plain spread reads as faded.
        [Theory]
        [InlineData("letter-a", "letter-a")]
        [InlineData("button", "button-enabled")]
        public void A_softer_screenshot_of_the_right_thing_is_still_found(string template, string screenshot)
        {
            using Mat picture = Picture(screenshot);
            using Mat softer = new Mat();
            Cv2.GaussianBlur(picture, softer, new Size(0, 0), 0.8);

            Match(template, Raw(softer), TemplateMatchModeEnum.SHAPE_AND_COLOUR).Matches.ShouldHaveSingleItem();
        }


        // ================================================================
        // Going on past a refusal
        // ================================================================

        // The live one a little softer, so the disabled one has the better shape and comes first.
        [Fact]
        public void A_refused_copy_with_the_better_shape_does_not_hide_the_live_one()
        {
            using Mat both = DisabledBesideASofterEnabled(out int half);

            TemplateMatchOutcome outcome = Match("button", Raw(both), TemplateMatchModeEnum.SHAPE_AND_COLOUR);

            TemplateMatchResult refused = outcome.Refused.ShouldHaveSingleItem();
            TemplateMatchResult hit = outcome.Matches.ShouldHaveSingleItem();
            refused.X.ShouldBeLessThan(half);
            hit.X.ShouldBeGreaterThanOrEqualTo(half);
            refused.Score.ShouldBeGreaterThan(hit.Score);
        }

        [Fact]
        public void Shape_takes_the_better_shape_whatever_state_it_is_in()
        {
            using Mat both = DisabledBesideASofterEnabled(out int half);

            Match("button", Raw(both), TemplateMatchModeEnum.SHAPE).Matches.ShouldHaveSingleItem().X.ShouldBeLessThan(half);
        }


        // ================================================================
        // What is compared
        // ================================================================

        // Erased pixels painted red: the colour would be far off if they counted.
        [Fact]
        public void Only_the_kept_pixels_are_compared()
        {
            TemplateMatchOutcome outcome = _openCv.Match(new TemplateMatchRequest
            {
                Haystack = Screenshot("button-enabled"),
                TemplateImage = WithRedErasedEdge("button"),
                Mode = TemplateMatchModeEnum.SHAPE_AND_COLOUR,
                AccuracyThreshold = ACCURACY,
                MaxMatches = 1,
            });

            TemplateMatchResult hit = outcome.Matches.ShouldHaveSingleItem();
            hit.Colour!.Value.ShouldBe(0f, COLOUR_PRECISION);
            hit.Contrast!.Value.ShouldBe(1f, CONTRAST_PRECISION);
        }

        // The colour is measured on the template as it was resized for the match.
        [Theory]
        [InlineData("button-enabled", true)]
        [InlineData("button-disabled", false)]
        public void A_scaled_template_is_compared_at_its_scaled_size(string screenshot, bool isFound)
        {
            using Mat picture = Picture(screenshot);
            using Mat larger = new Mat();
            Cv2.Resize(picture, larger, new Size(0, 0), 1.25, 1.25, InterpolationFlags.Linear);

            TemplateMatchOutcome outcome = Match("button", Raw(larger), TemplateMatchModeEnum.SHAPE_AND_COLOUR, scale: 1.25f);

            outcome.Matches.Count.ShouldBe(isFound ? 1 : 0);
            outcome.Refused.Count.ShouldBe(isFound ? 0 : 1);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private TemplateMatchOutcome Match(string template, RawImage screenshot, TemplateMatchModeEnum mode, float accuracy = ACCURACY, float scale = 1f)
        {
            return _openCv.Match(new TemplateMatchRequest
            {
                Haystack = screenshot,
                TemplateImage = Asset("Templates", template),
                Mode = mode,
                AccuracyThreshold = accuracy,
                ScaleRatio = scale,
                MaxMatches = 1,
            });
        }

        private static byte[] Asset(string folder, string name)
        {
            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", folder, name + ".png"));
        }

        private static Mat Picture(string name)
        {
            return Cv2.ImDecode(Asset("Screenshots", name), ImreadModes.Color);
        }

        private static RawImage Screenshot(string name)
        {
            using Mat picture = Picture(name);
            return Raw(picture);
        }

        private static RawImage Raw(Mat bgr)
        {
            using Mat bgra = bgr.CvtColor(ColorConversionCodes.BGR2BGRA);

            return new RawImage
            {
                Width = bgra.Width,
                Height = bgra.Height,
                Stride = bgra.Width * 4,
                Pixels = bgra.AsSpan<byte>().ToArray(),
            };
        }

        private static Mat DisabledBesideASofterEnabled(out int half)
        {
            using Mat disabled = Picture("button-disabled");
            using Mat enabled = Picture("button-enabled");
            using Mat softer = new Mat();
            Cv2.GaussianBlur(enabled, softer, new Size(0, 0), 0.6);

            Mat both = new Mat();
            Cv2.HConcat(disabled, softer, both);

            half = disabled.Width;
            return both;
        }

        // Drawn at an opacity over white, the way a page fades something out.
        private static Mat Faded(string name, double opacity)
        {
            using Mat picture = Picture(name);

            Mat faded = new Mat();
            picture.ConvertTo(faded, MatType.CV_8UC3, opacity, (1 - opacity) * 255);
            return faded;
        }

        private static byte[] WithRedErasedEdge(string name)
        {
            using Mat picture = Cv2.ImDecode(Asset("Templates", name), ImreadModes.Color);
            using Mat bgra = picture.CvtColor(ColorConversionCodes.BGR2BGRA);

            Rect edge = new Rect(0, 0, bgra.Width / 4, bgra.Height);
            using Mat erased = bgra[edge];
            erased.SetTo(new Scalar(0, 0, 255, 0));

            return bgra.ImEncode(".png");
        }
    }
}
