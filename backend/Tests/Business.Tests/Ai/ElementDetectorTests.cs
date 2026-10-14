using System.Drawing;
using Business.Ai.Vision;
using Business.Ai.Vision.Models;
using Core.Models.Business;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Business.Tests.Ai
{
    /// <summary>
    /// What the element detector is given and what its outputs mean, the way OmniParser's own
    /// decoder reads them - pinned here, so a change to either side shows.
    /// </summary>
    public sealed class ElementDetectorTests
    {
        private const int CELLS_8 = 80;
        private const int CELLS_16 = 40;
        private const int CELLS_32 = 20;

        // Outputs with every cell scored far below anything kept.
        private static List<Tensor<float>> Outputs()
        {
            List<Tensor<float>> outputs = new List<Tensor<float>>();

            foreach (int cells in new[] { CELLS_8, CELLS_16, CELLS_32 })
            {
                DenseTensor<float> scores = new DenseTensor<float>([1, 1, cells, cells]);
                scores.Fill(-20f);
                outputs.Add(scores);
                outputs.Add(new DenseTensor<float>([1, 4, cells, cells]));
            }

            return outputs;
        }

        private static void Score(List<Tensor<float>> outputs, int grid, int row, int column, float logit, float left, float top, float right, float bottom)
        {
            outputs[2 * grid][0, 0, row, column] = logit;
            outputs[2 * grid + 1][0, 0, row, column] = left;
            outputs[2 * grid + 1][0, 1, row, column] = top;
            outputs[2 * grid + 1][0, 2, row, column] = right;
            outputs[2 * grid + 1][0, 3, row, column] = bottom;
        }

        private static DetectedBox Box(float left, float top, float right, float bottom, float score)
        {
            return new DetectedBox { Bounds = RectangleF.FromLTRB(left, top, right, bottom), Score = score };
        }

        // ================================================================
        // The input
        // ================================================================

        [Fact]
        public void The_input_is_rgb_planes_from_0_to_1_and_grey_where_the_image_does_not_reach()
        {
            // One pixel, captured as blue 10, green 20, red 30.
            RawImage image = new RawImage { Width = 1, Height = 1, Stride = 4, Pixels = [10, 20, 30, 255] };

            DenseTensor<float> input = ElementDetector.BuildInput(image, new Rectangle(0, 0, 640, 640));

            input.Dimensions.ToArray().ShouldBe([1, 3, 640, 640]);
            (input[0, 0, 0, 0], input[0, 1, 0, 0], input[0, 2, 0, 0]).ShouldBe((30f / 255f, 20f / 255f, 10f / 255f));
            input[0, 0, 0, 1].ShouldBe(114f / 255f);
        }

        [Fact]
        public void The_input_starts_at_the_region()
        {
            byte[] pixels = new byte[3 * 3 * 4];
            pixels[(2 * 3 + 1) * 4 + 2] = 255;      // Red at x 1, y 2.
            RawImage image = new RawImage { Width = 3, Height = 3, Stride = 12, Pixels = pixels };

            DenseTensor<float> input = ElementDetector.BuildInput(image, new Rectangle(1, 2, 640, 640));

            input[0, 0, 0, 0].ShouldBe(1f);
        }

        // ================================================================
        // The outputs
        // ================================================================

        [Fact]
        public void A_cell_is_a_box_around_its_centre_its_distances_counted_in_cells()
        {
            List<Tensor<float>> outputs = Outputs();
            // The 16 pixel grid, row 2, column 3: its centre is at 56, 40.
            Score(outputs, grid: 1, row: 2, column: 3, logit: 0f, left: 1f, top: 0.5f, right: 2f, bottom: 1f);

            DetectedBox box = ElementDetector.Decode(outputs, 0.25f).ShouldHaveSingleItem();

            box.Bounds.ShouldBe(RectangleF.FromLTRB(40f, 32f, 88f, 56f));
            box.Score.ShouldBe(0.5f);
        }

        [Fact]
        public void A_cell_scored_under_the_minimum_is_left_out()
        {
            List<Tensor<float>> outputs = Outputs();
            Score(outputs, grid: 0, row: 0, column: 0, logit: -1.5f, left: 1f, top: 1f, right: 1f, bottom: 1f);

            ElementDetector.Decode(outputs, 0.25f).ShouldBeEmpty();
        }

        [Fact]
        public void Of_boxes_that_overlap_only_the_best_scored_is_kept()
        {
            DetectedBox best = Box(0, 0, 100, 50, 0.9f);
            DetectedBox same = Box(2, 2, 100, 50, 0.6f);
            DetectedBox apart = Box(200, 0, 300, 50, 0.4f);

            ElementDetector.Suppress([same, best, apart], 0.7f).ShouldBe([best, apart]);
        }

        // An icon inside a row: the click was on the icon.
        [Fact]
        public void The_element_under_a_click_is_the_smallest_box_it_is_in()
        {
            DetectedBox row = Box(0, 0, 400, 40, 0.9f);
            DetectedBox icon = Box(360, 5, 390, 35, 0.6f);

            ElementDetector.Under([row, icon], new PointF(375, 20), 3f).ShouldBe(icon);
            ElementDetector.Under([row, icon], new PointF(100, 20), 3f).ShouldBe(row);
            ElementDetector.Under([row, icon], new PointF(100, 60), 3f).ShouldBeNull();
        }
    }
}
