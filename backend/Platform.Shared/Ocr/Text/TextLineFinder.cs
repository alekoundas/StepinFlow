using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace Platform.Shared.Ocr.Text
{
    /// <summary>
    /// PP-OCRv5's line finder. 
    /// The input screenshot has to:
    /// 1) Drop the alpha channel.
    /// 2) Pass it via a tensor to the model and retrieve the Heatmap of where a letter possibly exist.
    /// 3) Make the Heatmap black-and-white by making each pixel white or black depending on the threshold.
    /// 4) Draw a outline with straight lines around the patch (White pixels that touch form a patch).
    ///    In training, PaddleOCR shrank every line text outline toward its middle(height and width) before showing it to the model as the right answer.
    ///    That causes the outline to be off by a couple of pixels and the outline needs to grow.
    /// 5) Remove noise.
    /// 6) Remove results where the average accuracy is less than the threshold.
    /// 7) Grow the box on every side to contain the whole line.
    /// 8) Convert the 4 box corners to the screenshot pixels (rounded).
    /// 
    /// </summary>
    internal sealed class TextLineFinder : IDisposable
    {
        private const int STRIDE = 32;              // The model halves the picture five times (2⁵ = 32).
        private const int SMALLEST_SIDE = 64;       // The smallest picture worth showing the model.
        private const int LARGEST_SIDE = 4000;      // The largest  picture worth showing the model. Past it, memory and time explode.
        private const float TEXT_THRESHOLD = 0.3f;  // A pixel counts as text above this.
        private const float BOX_THRESHOLD = 0.6f;   // A box is kept only if its average score is above this.
        private const float UNCLIP_RATIO = 1.5f;    // How far to grow the box to contain the whole line.(PaddleOCR was trained with smaller box on the line)
        private const int MAX_CANDIDATES = 1000;    // A cap, so a noisy picture can’t produce tens of thousands of shapes.
        private const int MIN_SIZE = 3;             // Shapes thinner than 3 pixels are specks.
        private const float SAME_ROW = 10f;         // Two boxes whose tops are within 10 pixels count as one row.

        // ImageNet’s colour statistics
        private static readonly float[] _mean = [0.485f, 0.456f, 0.406f];
        private static readonly float[] _std = [0.229f, 0.224f, 0.225f];

        private readonly InferenceSession _session; // Model session (model is in memory).
        private readonly string _inputName;         // Model single input.



        public TextLineFinder(InferenceSession session)
        {
            _session = session;
            _inputName = session.InputMetadata.Keys.First(); // Get the model single input.
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Generate the boxes with the text from the screenshot.
        /// </summary>
        public List<Point2f[]> Find(Mat bgr, double shrink)
        {
            int height = bgr.Rows;
            int width = bgr.Cols;

            // Find how much to scale by.
            double ratio = 1 / Math.Max(1, shrink);                     // Max(1, …) means a shrink below 1 never enlarges.
            if (Math.Min(height, width) * ratio < SMALLEST_SIDE)        // A small area, such as a 200×30 strip, would end up too small to see, so it’s enlarged.
                ratio = (double)SMALLEST_SIDE / Math.Min(height, width);
            if (Math.Max(height, width) * ratio > LARGEST_SIDE)         // The long side never goes past 4000.
                ratio = (double)LARGEST_SIDE / Math.Max(height, width);

            // Each side is rounded to the nearest multiple of 32, and is at least 32.
            // This can stretch the picture shape by a few pixels. Thats harmless, because the boxes are converted back with separate X and Y factors.
            int inputHeight = Math.Max((int)Math.Round(height * ratio / STRIDE) * STRIDE, STRIDE);
            int inputWidth = Math.Max((int)Math.Round(width * ratio / STRIDE) * STRIDE, STRIDE);

            // Generate the Heatmap.
            float[] heat = HeatMap(bgr, inputWidth, inputHeight);

            // The model answer is one float between 0 and 1 per pixel. Wraps it as a one-channel float image (CV_32FC1), so OpenCV can work on it without a copy.
            using Mat heatMap = Mat.FromPixelData(inputHeight, inputWidth, MatType.CV_32FC1, heat);

            // Generate the boxes with the text.
            List<Point2f[]> boxes = BoxesOf(heatMap, (float)width / inputWidth, (float)height / inputHeight, width, height);
            return Sorted(boxes);
        }

        public void Dispose()
        {
            _session.Dispose();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Hand the image tensor to the model input and retrieve the output shape of [1, 1, h, w]
        private float[] HeatMap(Mat bgr, int inputWidth, int inputHeight)
        {
            // Scale the screenshot to the model size, then copies the pixels into an array
            using Mat resized = new Mat();
            Cv2.Resize(bgr, resized, new Size(inputWidth, inputHeight));
            resized.GetArray(out Vec3b[] pixels);

            // The channels stay in OpenCV's blue, green, red order, which is what PaddleOCR trained on.
            DenseTensor<float> inputTensor = new DenseTensor<float>([1, 3, inputHeight, inputWidth]);
            Span<float> buffer = inputTensor.Buffer.Span;
            int plane = inputHeight * inputWidth;
            for (int i = 0; i < pixels.Length; i++)
            {
                buffer[i] = (pixels[i].Item0 / 255f - _mean[0]) / _std[0];
                buffer[plane + i] = (pixels[i].Item1 / 255f - _mean[1]) / _std[1];
                buffer[2 * plane + i] = (pixels[i].Item2 / 255f - _mean[2]) / _std[2];
            }

            // Ask the model.
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)]);
            return outputs[0].AsTensor<float>().ToArray();
        }

        // Generate the box of every text found.
        private static List<Point2f[]> BoxesOf(Mat heatMap, float scaleX, float scaleY, int width, int height)
        {
            using Mat bitmap = new Mat();

            // Makes a black-and-white picture. If pixel above 0.3 it becomes 255 else 0
            Cv2.Threshold(heatMap, bitmap, TEXT_THRESHOLD, 255, ThresholdTypes.Binary);
            bitmap.ConvertTo(bitmap, MatType.CV_8UC1); // 1-channel 8-bits

            // Finds the outline of every white patch (straight lines).
            Cv2.FindContours(bitmap, out Point[][] contours, out _, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            List<Point2f[]> boxes = new List<Point2f[]>();
            foreach (Point[] contour in contours.Take(MAX_CANDIDATES))
            {
                RotatedRect rect = Cv2.MinAreaRect(contour); // Get smallest outline.

                // Filter - Remove noise.
                if (Math.Min(rect.Size.Width, rect.Size.Height) < MIN_SIZE)
                    continue;

                // Filter - Average threshold compare 
                if (BoxScore(heatMap, Corners(rect)) < BOX_THRESHOLD)
                    continue;

                // Model generates a smaller heatmap that the actual line.
                // Box grown by as much as the model shrank it: PaddleOCR's offset, area × ratio / perimeter.
                float area = rect.Size.Width * rect.Size.Height;
                float perimeter = 2 * (rect.Size.Width + rect.Size.Height);
                float grow = area * UNCLIP_RATIO / perimeter;
                RotatedRect line = new RotatedRect(rect.Center, new Size2f(rect.Size.Width + 2 * grow, rect.Size.Height + 2 * grow), rect.Angle);
                if (Math.Min(line.Size.Width, line.Size.Height) < MIN_SIZE + 2)
                    continue;

                // Convert the 4 corners to the screenshot pixels and round them.
                boxes.Add(Corners(line)
                    .Select(p => new Point2f(Math.Clamp(MathF.Round(p.X * scaleX), 0, width), Math.Clamp(MathF.Round(p.Y * scaleY), 0, height)))
                    .ToArray());
            }

            return boxes;
        }

        // Workout the corners and order them clockwise as PaddleOCR does.
        private static Point2f[] Corners(RotatedRect rect)
        {
            Point2f[] points = rect.Points().OrderBy(x => x.X).ToArray();

            int topLeft = 1;
            int bottomLeft = 0;
            if (points[1].Y > points[0].Y)
            {
                topLeft = 0;
                bottomLeft = 1;
            }

            int topRight = 3;
            int bottomRight = 2;
            if (points[3].Y > points[2].Y)
            {
                topRight = 2;
                bottomRight = 3;
            }

            return [points[topLeft], points[topRight], points[bottomRight], points[bottomLeft]];
        }

        // Find the score the of the box on the heatmap.
        private static double BoxScore(Mat heatMap, Point2f[] box)
        {
            // Find the box in the hetamap.
            int left = Math.Clamp((int)MathF.Floor(box.Min(p => p.X)), 0, heatMap.Cols - 1);
            int right = Math.Clamp((int)MathF.Ceiling(box.Max(p => p.X)), 0, heatMap.Cols - 1);
            int top = Math.Clamp((int)MathF.Floor(box.Min(p => p.Y)), 0, heatMap.Rows - 1);
            int bottom = Math.Clamp((int)MathF.Ceiling(box.Max(p => p.Y)), 0, heatMap.Rows - 1);

            // Create a new mat with a full white box in the same area as the heat maps shows it.
            using Mat mask = Mat.Zeros(bottom - top + 1, right - left + 1, MatType.CV_8UC1);
            Point[] shifted = box.Select(p => new Point((int)MathF.Round(p.X - left), (int)MathF.Round(p.Y - top))).ToArray();
            Cv2.FillPoly(mask, [shifted], Scalar.All(1));

            // Compare the mask with the heatmap and average the whites.
            using Mat region = new Mat(heatMap, new Rect(left, top, right - left + 1, bottom - top + 1));
            return Cv2.Mean(region, mask).Val0;
        }

        // Top to bottom, and left to right within a few pixels of the same height, as PaddleOCR sorts.
        private static List<Point2f[]> Sorted(List<Point2f[]> boxes)
        {
            List<Point2f[]> sorted = boxes.OrderBy(x => x[0].Y).ThenBy(x => x[0].X).ToList();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                for (int j = i; j >= 0; j--)
                {
                    bool isSameRow = Math.Abs(sorted[j + 1][0].Y - sorted[j][0].Y) < SAME_ROW;
                    if (!isSameRow || sorted[j + 1][0].X >= sorted[j][0].X)
                        break;

                    (sorted[j], sorted[j + 1]) = (sorted[j + 1], sorted[j]);
                }
            }

            return sorted;
        }
    }
}
