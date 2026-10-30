using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Core.Models.Business;
using Platform.Windows.Ocr;

namespace Platform.Windows.Tests.Ocr
{
    /// <summary>
    /// Windows OCR on a picture drawn here, so where every line is, is known. Needs the OCR of a
    /// language in the user's profile, which every Windows install has.
    /// </summary>
    public sealed class OcrServiceTests
    {
        private const int WIDTH = 1400;
        private const int HEIGHT = 800;
        private const double TOLERANCE = 12;

        // Each on a row of its own, away from the centre, where a turn moves a line furthest.
        private static readonly (string Text, PointF Centre)[] LINES =
        [
            ("Battle pass opens every week", new PointF(350, 100)),
            ("Guild hall research continues", new PointF(1050, 210)),
            ("Market prices change at noon", new PointF(350, 320)),
            ("Upgrades for every hero today", new PointF(1050, 480)),
            ("Select a firestone research", new PointF(350, 590)),
            ("Fellowship party starts soon", new PointF(1050, 700)),
        ];

        // Tilted far enough, the engine straightens the text and measures in the straightened
        // picture; a line 350 pixels from the centre would be read 37 pixels off at 6 degrees.
        [Theory]
        [InlineData(0f)]
        [InlineData(6f)]
        [InlineData(-6f)]
        public async Task Every_line_is_where_it_was_drawn(float tilt)
        {
            (RawImage image, List<PointF> centres) = Draw(tilt);

            IReadOnlyList<OcrWord> words = await new OcrService().ReadWordsAsync(image, string.Empty, TestContext.Current.CancellationToken);

            for (int i = 0; i < LINES.Length; i++)
            {
                string first = LINES[i].Text.Split(' ')[0];
                int line = words.First(x => string.Equals(x.Text, first, StringComparison.OrdinalIgnoreCase)).LineIndex;
                Rectangle read = words.Where(x => x.LineIndex == line).Select(x => x.Bounds).Aggregate(Rectangle.Union);

                double dx = read.Left + read.Width / 2d - centres[i].X;
                double dy = read.Top + read.Height / 2d - centres[i].Y;
                Math.Sqrt(dx * dx + dy * dy).ShouldBeLessThan(TOLERANCE, $"\"{LINES[i].Text}\" was drawn around {centres[i]} and read at {read}");
            }
        }

        private static (RawImage Image, List<PointF> Centres) Draw(float tilt)
        {
            using Bitmap bitmap = new Bitmap(WIDTH, HEIGHT, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bitmap);
            using Font font = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel);
            using Matrix tilted = new Matrix();

            graphics.Clear(Color.White);
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
            tilted.RotateAt(tilt, new PointF(WIDTH / 2f, HEIGHT / 2f));
            graphics.Transform = tilted;

            List<PointF> centres = new List<PointF>();
            foreach ((string text, PointF centre) in LINES)
            {
                SizeF size = graphics.MeasureString(text, font);
                graphics.DrawString(text, font, Brushes.Black, centre.X - size.Width / 2, centre.Y - size.Height / 2);

                PointF[] drawn = [centre];
                tilted.TransformPoints(drawn);
                centres.Add(drawn[0]);
            }

            return (ToRawImage(bitmap), centres);
        }

        private static RawImage ToRawImage(Bitmap bitmap)
        {
            Rectangle whole = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(whole, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                byte[] pixels = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                return new RawImage { Width = bitmap.Width, Height = bitmap.Height, Stride = data.Stride, Pixels = pixels };
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}
