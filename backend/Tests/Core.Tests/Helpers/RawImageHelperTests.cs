using System.Drawing;

using Core.Helpers;
using Core.Models.Business;

namespace Core.Tests.Helpers
{
    public sealed class RawImageHelperTests
    {
        // A 4x3 screenshot at 100,200 whose every pixel's blue byte is its own x + 10 * y, and with
        // two bytes of padding on each row, as a capture can have.
        private static readonly Rectangle BOUNDS = new Rectangle(100, 200, 4, 3);

        private static RawImage Screenshot()
        {
            int stride = 4 * 4 + 2;
            byte[] pixels = new byte[stride * 3];

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 4; x++)
                    pixels[y * stride + x * 4] = (byte)(x + 10 * y);
            }

            return new RawImage { Width = 4, Height = 3, Stride = stride, Pixels = pixels };
        }

        private static List<byte> Blues(RawImage image)
        {
            List<byte> blues = new List<byte>();

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                    blues.Add(image.Pixels[y * image.Stride + x * 4]);
            }

            return blues;
        }

        [Fact]
        public void A_region_inside_comes_back_pixel_for_pixel()
        {
            RawImage cropped = RawImageHelper.Crop(Screenshot(), BOUNDS, new Rectangle(101, 201, 2, 2));

            (cropped.Width, cropped.Height, cropped.Stride).ShouldBe((2, 2, 8));
            Blues(cropped).ShouldBe([11, 12, 21, 22]);
        }

        [Fact]
        public void A_region_over_the_edge_keeps_only_the_part_inside()
        {
            RawImage cropped = RawImageHelper.Crop(Screenshot(), BOUNDS, new Rectangle(102, 190, 50, 12));

            (cropped.Width, cropped.Height).ShouldBe((2, 2));
            Blues(cropped).ShouldBe([2, 3, 12, 13]);
        }

        [Fact]
        public void A_region_outside_is_empty()
        {
            RawImageHelper.Crop(Screenshot(), BOUNDS, new Rectangle(0, 0, 50, 50)).IsEmpty.ShouldBeTrue();
        }

        // Two monitors side by side, and a region lying across the seam between them.
        [Fact]
        public void A_region_across_two_screenshots_takes_each_part_from_its_own()
        {
            Rectangle region = new Rectangle(102, 201, 4, 1);
            RawImage target = new RawImage { Width = 4, Height = 1, Stride = 16, Pixels = new byte[16] };

            RawImageHelper.Paste(target, region, Screenshot(), BOUNDS);
            RawImageHelper.Paste(target, region, Screenshot(), new Rectangle(104, 200, 4, 3));

            Blues(target).ShouldBe([12, 13, 10, 11]);
        }
    }
}
