using System.Drawing;
using Business.Ai.Vision.Models;
using Core.Models.Business;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Business.Ai.Vision
{
    /// <summary>
    /// What OmniParser's element detector, a YOLOv9-E, is given and what its outputs mean - the
    /// way its own util/yolov9.py reads them.
    ///
    /// In: one RGB image of 640 by 640, each colour a plane of its own, values 0 to 1. What the
    /// screenshot does not cover is the grey a letterbox pads with.
    /// Out: three grids, of 8, 16 and 32 pixel cells, in that order. Each is a pair: a score per
    /// cell, for an element centred there, and four distances from the cell's centre to the
    /// element's left, top, right and bottom, counted in cells.
    /// </summary>
    public static class ElementDetector
    {
        public const int INPUT_SIZE = 640;

        private static readonly int[] STRIDES = [8, 16, 32];
        private const float PADDING = 114f / 255f;
        private const int MAXIMUM_BOXES = 300;

        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>The region of the image as the model's input.</summary>
        public static DenseTensor<float> BuildInput(RawImage image, Rectangle region)
        {
            int plane = INPUT_SIZE * INPUT_SIZE;
            float[] input = new float[3 * plane];
            Array.Fill(input, PADDING);

            Rectangle inside = Rectangle.Intersect(region, new Rectangle(0, 0, image.Width, image.Height));

            for (int y = inside.Top; y < inside.Bottom; y++)
            {
                int row = (y - region.Y) * INPUT_SIZE;

                for (int x = inside.Left; x < inside.Right; x++)
                {
                    // Captured as BGRA, given as RGB.
                    int from = y * image.Stride + x * 4;
                    int to = row + x - region.X;

                    input[to] = image.Pixels[from + 2] / 255f;
                    input[plane + to] = image.Pixels[from + 1] / 255f;
                    input[2 * plane + to] = image.Pixels[from] / 255f;
                }
            }

            return new DenseTensor<float>(input, [1, 3, INPUT_SIZE, INPUT_SIZE]);
        }

        /// <summary>Every cell scored above the minimum, as a box in the input's pixels.</summary>
        public static List<DetectedBox> Decode(IReadOnlyList<Tensor<float>> outputs, float minimumScore)
        {
            List<DetectedBox> boxes = new List<DetectedBox>();

            // Compared before the sigmoid, so a cell under the minimum costs no exponent.
            float minimumLogit = MathF.Log(minimumScore / (1f - minimumScore));

            for (int grid = 0; grid < STRIDES.Length; grid++)
            {
                Tensor<float> scores = outputs[2 * grid];
                Tensor<float> distances = outputs[2 * grid + 1];
                int stride = STRIDES[grid];
                int rows = scores.Dimensions[2];
                int columns = scores.Dimensions[3];

                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        float logit = scores[0, 0, row, column];
                        if (logit <= minimumLogit)
                            continue;

                        float centreX = (column + 0.5f) * stride;
                        float centreY = (row + 0.5f) * stride;
                        float left = centreX - distances[0, 0, row, column] * stride;
                        float top = centreY - distances[0, 1, row, column] * stride;
                        float right = centreX + distances[0, 2, row, column] * stride;
                        float bottom = centreY + distances[0, 3, row, column] * stride;

                        boxes.Add(new DetectedBox
                        {
                            Bounds = RectangleF.FromLTRB(left, top, right, bottom),
                            Score = 1f / (1f + MathF.Exp(-logit)),
                        });
                    }
                }
            }

            return boxes;
        }

        /// <summary>
        /// Non-maximum suppression: neighbouring cells see the same element, so of boxes that
        /// overlap more than the maximum, only the best scored is kept.
        /// </summary>
        public static List<DetectedBox> Suppress(IReadOnlyList<DetectedBox> boxes, float maximumOverlap)
        {
            List<DetectedBox> kept = new List<DetectedBox>();

            foreach (DetectedBox box in boxes.OrderByDescending(x => x.Score))
            {
                if (kept.Count == MAXIMUM_BOXES)
                    break;

                if (kept.All(x => Overlap(x.Bounds, box.Bounds) <= maximumOverlap))
                    kept.Add(box);
            }

            return kept;
        }

        /// <summary>The smallest box the point is in, so a click on an icon in a row is the icon.</summary>
        public static DetectedBox? Under(IReadOnlyList<DetectedBox> boxes, PointF point, float tolerance)
        {
            DetectedBox? under = null;

            foreach (DetectedBox box in boxes)
            {
                RectangleF around = RectangleF.Inflate(box.Bounds, tolerance, tolerance);
                if (!around.Contains(point))
                    continue;

                if (under == null || Area(box.Bounds) < Area(under.Bounds))
                    under = box;
            }

            return under;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Intersection over union.
        private static float Overlap(RectangleF a, RectangleF b)
        {
            RectangleF both = RectangleF.Intersect(a, b);
            if (both.IsEmpty)
                return 0f;

            float shared = Area(both);
            return shared / (Area(a) + Area(b) - shared);
        }

        private static float Area(RectangleF rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }
    }
}
