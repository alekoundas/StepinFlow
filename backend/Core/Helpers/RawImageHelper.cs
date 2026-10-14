using System.Drawing;

using Core.Models.Business;

namespace Core.Helpers
{
    public static class RawImageHelper
    {
        private const int BYTES_PER_PIXEL = 4;

        /// <summary>
        /// The part of a screenshot inside a region, both in screen coordinates. Whatever of the
        /// region falls outside the screenshot is left out, so the result can be smaller than asked,
        /// or empty.
        /// </summary>
        public static RawImage Crop(RawImage image, Rectangle imageBounds, Rectangle region)
        {
            Rectangle inside = Rectangle.Intersect(imageBounds, region);
            if (inside.Width <= 0 || inside.Height <= 0 || image.IsEmpty)
                return new RawImage();

            int left = inside.X - imageBounds.X;
            int top = inside.Y - imageBounds.Y;
            int rowLength = inside.Width * BYTES_PER_PIXEL;

            RawImage cropped = new RawImage
            {
                Width = inside.Width,
                Height = inside.Height,
                Stride = rowLength,
                Pixels = new byte[rowLength * inside.Height],
            };

            for (int row = 0; row < inside.Height; row++)
            {
                int from = (top + row) * image.Stride + left * BYTES_PER_PIXEL;
                Buffer.BlockCopy(image.Pixels, from, cropped.Pixels, row * rowLength, rowLength);
            }

            return cropped;
        }

        /// <summary>Each pixel repeated factor times across and down, with nothing blended.</summary>
        public static RawImage Enlarge(RawImage image, int factor)
        {
            int width = image.Width * factor;
            int height = image.Height * factor;
            int rowLength = width * BYTES_PER_PIXEL;

            RawImage enlarged = new RawImage
            {
                Width = width,
                Height = height,
                Stride = rowLength,
                Pixels = new byte[rowLength * height],
            };

            for (int y = 0; y < height; y++)
            {
                int from = y / factor * image.Stride;
                int to = y * rowLength;

                for (int x = 0; x < width; x++)
                    Buffer.BlockCopy(image.Pixels, from + x / factor * BYTES_PER_PIXEL, enlarged.Pixels, to + x * BYTES_PER_PIXEL, BYTES_PER_PIXEL);
            }

            return enlarged;
        }
    }
}
