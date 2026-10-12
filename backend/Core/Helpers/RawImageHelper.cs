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
    }
}
