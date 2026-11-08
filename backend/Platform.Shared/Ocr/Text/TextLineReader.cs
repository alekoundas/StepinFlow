using System.Drawing;
using Core.Models.Business;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace Platform.Shared.Ocr.Text
{
    /// <summary>
    /// One of PP-OCRv5's readers. 
    /// The generated line boxes will go though:
    /// 1) Level each line box with "perspective transform" because box might be uneven/lopsided.
    /// 2) Crop the box from the image.
    /// 3) Tranform the crop to 48 pixels tall (also convert the width). ex a crop of 300 × 20 becomes 720 × 48.
    /// 4) Model answers with the slices (every character recognized in that crop).
    /// 
    /// One line at a time: on a CPU six lines at once took longer than six one by one.
    /// </summary>
    internal sealed class TextLineReader : IDisposable
    {
        private const int HEIGHT = 48;          // Line box should be 48 pixels tall.
        private const int MIN_WIDTH = 320;      // a short word is padded to it.

        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string[] _readerCharacters;

        public TextLineReader(InferenceSession session, string[] characters)
        {
            int classes = session.OutputMetadata.First().Value.Dimensions[^1];
            if (classes != characters.Length)
                throw new InvalidDataException($"The reader tells {classes} characters apart and its list has {characters.Length}.");

            _session = session;
            _inputName = session.InputMetadata.Keys.First();
            _readerCharacters = characters;
        }

        public void Dispose()
        {
            _session.Dispose();
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Every word on the lines, line by line, each line's words left to right.</summary>
        public List<OcrWord> Read(Mat bgr, List<Point2f[]> linesBoxes)
        {
            List<OcrWord> words = new List<OcrWord>();
            for (int index = 0; index < linesBoxes.Count; index++)
            {
                // Cuts this line out of the screenshot
                using Mat crop = Crop(bgr, linesBoxes[index]);

                // The model reads the line and get back each character, plus the slice it was seen in.
                List<(string Character, int Slice)> read = ReadLine(crop, out double sliceWidth);

                // Converts the characters into words and works out each word box.
                words.AddRange(WordsOf(read, crop, linesBoxes[index], sliceWidth, index));
            }

            return words;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Cut out level: a tilted line, a game's slanted title, comes out straight.
        private static Mat Crop(Mat bgr, Point2f[] corners)
        {
            // Measure the box accurately even if tilted.
            int width = Math.Max(1, (int)Math.Max(Distance(corners[0], corners[1]), Distance(corners[2], corners[3])));
            int height = Math.Max(1, (int)Math.Max(Distance(corners[0], corners[3]), Distance(corners[1], corners[2])));

            // Level the box with "perspective transform" because box might be uneven/lopsided.
            Point2f[] level = [new Point2f(0, 0), new Point2f(width, 0), new Point2f(width, height), new Point2f(0, height)];
            using Mat transform = Cv2.GetPerspectiveTransform(corners, level);

            // Generate new image cut out of the box.
            Mat image = new Mat();
            Cv2.WarpPerspective(bgr, image, transform, new OpenCvSharp.Size(width, height), InterpolationFlags.Cubic, BorderTypes.Replicate);

            return image;
        }

        // 
        private List<(string Character, int Slice)> ReadLine(Mat crop, out double sliceWidth)
        {
            // The line width calculated after the height is scaled to 48 pixels tall. ex a crop of 300 × 20 becomes 720 × 48.
            int resizedWidth = Math.Max(1, (int)Math.Ceiling(HEIGHT * (double)crop.Cols / crop.Rows));
            int inputWidth = Math.Max(MIN_WIDTH, resizedWidth);

            // It scales the crop, then copies its pixels into an array of blue-green-red triples, row by row.
            using Mat resized = new Mat();
            Cv2.Resize(crop, resized, new OpenCvSharp.Size(resizedWidth, HEIGHT));
            resized.GetArray(out Vec3b[] pixels);

            // Coppies each pixel into the Tensor
            // Scaled to -1..1; the padding past the line stays 0, a mid grey.
            DenseTensor<float> input = new DenseTensor<float>([1, 3, HEIGHT, inputWidth]);
            Span<float> buffer = input.Buffer.Span;
            int plane = HEIGHT * inputWidth;
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < resizedWidth; x++)
                {
                    Vec3b pixel = pixels[y * resizedWidth + x];
                    int at = y * inputWidth + x;
                    buffer[at] = pixel.Item0 / 127.5f - 1;
                    buffer[plane + at] = pixel.Item1 / 127.5f - 1;
                    buffer[2 * plane + at] = pixel.Item2 / 127.5f - 1;
                }
            }

            // The model answers with shape [1, slices, classes].
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, input)]);
            Tensor<float> output = outputs[0].AsTensor<float>();
            int slices = output.Dimensions[1];
            int classes = output.Dimensions[2];
            float[] guesses = output.ToArray();

            // How wide one slice is in the crop own pixels.
            sliceWidth = (double)inputWidth / slices * crop.Cols / resizedWidth;

            // For each slice, it finds the class with the highest score.
            List<(string Character, int Slice)> read = new List<(string, int)>();
            int previous = 0;
            for (int slice = 0; slice < slices; slice++)
            {
                int row = slice * classes;
                int best = 0;
                for (int index = 1; index < classes; index++)
                {
                    if (guesses[row + index] > guesses[row + best])
                        best = index;
                }

                if (best != 0 && best != previous)
                    read.Add((_readerCharacters[best], slice));

                previous = best;
            }

            return read;
        }

        // Merge slices to words split by space. A word's box runs between the blank columns
        // that part it from its neighbours, found from where their characters were seen.
        private static IEnumerable<OcrWord> WordsOf(List<(string Character, int Slice)> read, Mat crop, Point2f[] corners, double sliceWidth, int lineIndex)
        {

            // Merge the characters at the spaces the model read.
            List<List<(string Character, int Slice)>> words = new List<List<(string, int)>>();
            List<(string Character, int Slice)> current = new List<(string, int)>();
            foreach ((string Character, int Slice) character in read)
            {
                if (character.Character != " ")
                {
                    current.Add(character);
                    continue;
                }

                if (current.Count > 0)
                    words.Add(current);
                current = new List<(string, int)>();
            }

            // Add word.
            if (current.Count > 0)
                words.Add(current);

            if (words.Count == 0)
                yield break;

            using Mat gray = new Mat();
            Cv2.CvtColor(crop, gray, ColorConversionCodes.BGR2GRAY);
            gray.GetArray(out byte[] grayPixels);
            bool[,] ink = WordEdges.InkOf(grayPixels, gray.Cols, gray.Rows);
            bool[] columns = WordEdges.ColumnsOf(ink);

            for (int i = 0; i < words.Count; i++)
            {
                int firstCentre = (int)((words[i][0].Slice + 0.5) * sliceWidth);
                int lastCentre = (int)((words[i][^1].Slice + 0.5) * sliceWidth);

                int left;
                if (i == 0)
                {
                    left = WordEdges.FirstInk(columns, firstCentre);
                }
                else
                {
                    (int gapStart, int gapEnd) = WordEdges.WidestGap(columns, (int)((words[i - 1][^1].Slice + 0.5) * sliceWidth), firstCentre);
                    left = gapStart < 0 ? -1 : gapEnd + 1;
                }

                int right;
                if (i == words.Count - 1)
                {
                    right = WordEdges.LastInk(columns, lastCentre);
                }
                else
                {
                    (int gapStart, _) = WordEdges.WidestGap(columns, lastCentre, (int)((words[i + 1][0].Slice + 0.5) * sliceWidth));
                    right = gapStart < 0 ? -1 : gapStart - 1;
                }

                // No blank column to go by - letters touching their neighbours: the slices it was seen in.
                if (left < 0 || right < left)
                {
                    left = (int)(words[i][0].Slice * sliceWidth);
                    right = (int)((words[i][^1].Slice + 1) * sliceWidth) - 1;
                }

                (int top, int bottom) = WordEdges.RowsOf(ink, left, right);

                yield return new OcrWord
                {
                    Text = string.Concat(words[i].Select(x => x.Character)),
                    Bounds = InImage(corners, crop, left, top, right + 1, bottom + 1),
                    LineIndex = lineIndex,
                };
            }
        }

        // A rectangle in the crop, back in the image: through the line's tilt, the box around it.
        private static Rectangle InImage(Point2f[] corners, Mat crop, double left, double top, double right, double bottom)
        {
            PointF[] points = [At(corners, crop, left, top), At(corners, crop, right, top), At(corners, crop, right, bottom), At(corners, crop, left, bottom)];

            int x0 = (int)Math.Floor(points.Min(p => p.X));
            int y0 = (int)Math.Floor(points.Min(p => p.Y));
            int x1 = (int)Math.Ceiling(points.Max(p => p.X));
            int y1 = (int)Math.Ceiling(points.Max(p => p.Y));

            return Rectangle.FromLTRB(x0, y0, x1, y1);
        }

        // A point in the crop, back in the image: along the line's top edge and down its left one.
        private static PointF At(Point2f[] corners, Mat crop, double x, double y)
        {
            double across = x / crop.Cols;
            double down = y / crop.Rows;

            return new PointF(
                (float)(corners[0].X + (corners[1].X - corners[0].X) * across + (corners[3].X - corners[0].X) * down),
                (float)(corners[0].Y + (corners[1].Y - corners[0].Y) * across + (corners[3].Y - corners[0].Y) * down));
        }

        private static double Distance(Point2f a, Point2f b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }
    }
}
