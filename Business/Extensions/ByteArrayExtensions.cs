using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Business.Extensions
{
    public static class ByteArrayExtensions
    {
        public static BitmapSource ToBitmapSource(this byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is empty or null");

            using (var ms = new MemoryStream(imageData))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze(); // Allows cross-thread access
                return bitmap;
            }
        }

        public static byte[] ToByteArray(this Image image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            using MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public static Bitmap ToBitmap(this BitmapSource source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Create a new Bitmap with the same dimensions.
            Bitmap bitmap = new Bitmap(source.PixelWidth, source.PixelHeight, PixelFormat.Format32bppPArgb);

            // Lock the bitmap's bits for writing
            BitmapData data = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            // Copy the BitmapSource pixels to the Bitmap.
            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);

            // Unlock the bits
            bitmap.UnlockBits(data);

            return bitmap;
        }


    }
}
