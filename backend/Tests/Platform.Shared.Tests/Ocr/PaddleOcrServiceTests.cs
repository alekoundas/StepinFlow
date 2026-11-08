using Core.Catalogs;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Dtos;
using OpenCvSharp;
using Platform.Shared.Ocr;

namespace Platform.Shared.Tests.Ocr
{
    /// <summary>
    /// PaddleOCR on pictures drawn here, so what every line says and where it is, is known. Needs
    /// the line finder and the Latin reader in AiModels, which `npm run models` fetches.
    /// </summary>
    public sealed class PaddleOcrServiceTests : IDisposable
    {
        private const int WIDTH = 1400;
        private const int HEIGHT = 800;
        private const double TOLERANCE = 12;

        private static readonly string BUILT_IN = PathHelper.GetAiModelsPath();

        // Each on a row of its own, away from the centre, where a turn moves a line furthest.
        private static readonly (string Text, Point2f Centre)[] LINES =
        [
            ("Battle pass opens every week", new Point2f(350, 100)),
            ("Guild hall research continues", new Point2f(1050, 210)),
            ("Market prices change at noon", new Point2f(350, 320)),
            ("Upgrades for every hero today", new Point2f(1050, 480)),
            ("Select a firestone research", new Point2f(350, 590)),
            ("Fellowship party starts soon", new Point2f(1050, 700)),
        ];

        private readonly string _downloads = Path.Combine(Path.GetTempPath(), "StepinFlowTests", Guid.NewGuid().ToString("N"));
        private readonly PaddleOcrService _ocr;

        public PaddleOcrServiceTests()
        {
            Directory.CreateDirectory(_downloads);
            _ocr = new PaddleOcrService(BUILT_IN, _downloads);
        }

        public void Dispose()
        {
            _ocr.Dispose();
            Directory.Delete(_downloads, recursive: true);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(6f)]
        [InlineData(-6f)]
        public async Task Every_line_is_read_and_found_where_it_was_drawn(float tilt)
        {
            SkipWithoutModels();
            (RawImage image, List<Point2f> centres) = Draw(tilt, 1);

            IReadOnlyList<OcrWord> words = await _ocr.ReadWordsAsync(image, string.Empty, 96, TestContext.Current.CancellationToken);

            for (int i = 0; i < LINES.Length; i++)
            {
                string first = LINES[i].Text.Split(' ')[0];
                int line = words.First(x => x.Text == first).LineIndex;
                List<OcrWord> onLine = words.Where(x => x.LineIndex == line).ToList();

                string.Join(' ', onLine.Select(x => x.Text)).ShouldBe(LINES[i].Text);

                System.Drawing.Rectangle read = onLine.Select(x => x.Bounds).Aggregate(System.Drawing.Rectangle.Union);
                double dx = read.Left + read.Width / 2d - centres[i].X;
                double dy = read.Top + read.Height / 2d - centres[i].Y;
                Math.Sqrt(dx * dx + dy * dy).ShouldBeLessThan(TOLERANCE, $"\"{LINES[i].Text}\" was drawn around {centres[i]} and read at {read}");
            }
        }

        // Words left to right, apart: the gap between two is blank columns in the picture.
        [Fact]
        public async Task Each_word_has_a_box_of_its_own()
        {
            SkipWithoutModels();
            (RawImage image, _) = Draw(0, 1);

            IReadOnlyList<OcrWord> words = await _ocr.ReadWordsAsync(image, string.Empty, 96, TestContext.Current.CancellationToken);

            List<OcrWord> battle = words.Where(x => x.LineIndex == words.First(w => w.Text == "Battle").LineIndex).ToList();
            battle.Count.ShouldBe(5);
            for (int i = 1; i < battle.Count; i++)
                battle[i].Bounds.Left.ShouldBeGreaterThan(battle[i - 1].Bounds.Right);
        }

        // At 150% the text is drawn half as large again; the line finder looks at it shrunk back.
        [Fact]
        public async Task A_screen_at_150_percent_reads_the_same()
        {
            SkipWithoutModels();
            (RawImage image, _) = Draw(0, 1.5);

            string text = await _ocr.ReadAsync(image, string.Empty, 144, TestContext.Current.CancellationToken);

            foreach ((string line, _) in LINES)
                text.ShouldContain(line);
        }

        [Fact]
        public async Task An_empty_picture_reads_nothing()
        {
            (await _ocr.ReadAsync(new RawImage(), string.Empty, 96, TestContext.Current.CancellationToken)).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_reader_not_downloaded_says_where_to_get_it()
        {
            SkipWithoutModels();
            (RawImage image, _) = Draw(0, 1);

            InvalidOperationException refused = await Should.ThrowAsync<InvalidOperationException>(() => _ocr.ReadAsync(image, "greek", 96, TestContext.Current.CancellationToken));

            refused.Message.ShouldContain("Greek");
            refused.Message.ShouldContain("Settings");
        }

        [Fact]
        public async Task A_reader_the_app_does_not_know_is_refused()
        {
            (RawImage image, _) = Draw(0, 1);

            await Should.ThrowAsync<InvalidOperationException>(() => _ocr.ReadAsync(image, "klingon", 96, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Latin_is_built_in_and_every_other_reader_is_offered()
        {
            SkipWithoutModels();

            IReadOnlyList<OcrLanguageDto> languages = _ocr.GetLanguages();

            languages.Select(x => x.Tag).ShouldBe(OcrReaderCatalog.All.Select(x => x.Id));
            OcrLanguageDto latin = languages.Single(x => x.Tag == OcrReaderCatalog.DEFAULT_READER);
            (latin.IsInstalled, latin.IsBuiltIn).ShouldBe((true, true));
            languages.Where(x => x.Tag != OcrReaderCatalog.DEFAULT_READER).ShouldAllBe(x => !x.IsInstalled && !x.IsBuiltIn && x.Megabytes > 0);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void SkipWithoutModels()
        {
            Assert.SkipUnless(File.Exists(Path.Combine(BUILT_IN, OcrReaderCatalog.LINE_FINDER_FILE_NAME)), "The text recognition models are not in AiModels. Run npm run models.");
        }

        // Black on white, turned about the centre by the tilt, at a scale as a scaled screen draws it.
        private static (RawImage Image, List<Point2f> Centres) Draw(float tilt, double scale)
        {
            int width = (int)(WIDTH * scale);
            int height = (int)(HEIGHT * scale);
            using Mat canvas = new Mat(height, width, MatType.CV_8UC4, Scalar.All(255));

            foreach ((string text, Point2f centre) in LINES)
            {
                Size size = Cv2.GetTextSize(text, HersheyFonts.HersheyDuplex, 1.3 * scale, (int)Math.Round(2 * scale), out _);
                Point origin = new Point((int)(centre.X * scale - size.Width / 2d), (int)(centre.Y * scale + size.Height / 2d));
                Cv2.PutText(canvas, text, origin, HersheyFonts.HersheyDuplex, 1.3 * scale, new Scalar(0, 0, 0, 255), (int)Math.Round(2 * scale), LineTypes.AntiAlias);
            }

            Point2f middle = new Point2f(width / 2f, height / 2f);
            using Mat turn = Cv2.GetRotationMatrix2D(middle, tilt, 1);
            using Mat turned = new Mat();
            Cv2.WarpAffine(canvas, turned, turn, canvas.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(255));

            List<Point2f> centres = new List<Point2f>();
            foreach ((_, Point2f centre) in LINES)
            {
                double x = centre.X * scale;
                double y = centre.Y * scale;
                centres.Add(new Point2f(
                    (float)(turn.At<double>(0, 0) * x + turn.At<double>(0, 1) * y + turn.At<double>(0, 2)),
                    (float)(turn.At<double>(1, 0) * x + turn.At<double>(1, 1) * y + turn.At<double>(1, 2))));
            }

            byte[] pixels = new byte[width * height * 4];
            System.Runtime.InteropServices.Marshal.Copy(turned.Data, pixels, 0, pixels.Length);

            return (new RawImage { Pixels = pixels, Width = width, Height = height, Stride = width * 4 }, centres);
        }
    }
}
