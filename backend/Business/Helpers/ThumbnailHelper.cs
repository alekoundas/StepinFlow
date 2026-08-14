//using System.Drawing;
//using System.Drawing.Imaging;

//namespace Business.Helpers
//{
//    public static class ThumbnailHelper
//    {
//        private const int MaxSize = 48;

//        /// <summary>
//        /// Shrinks a template to something a tree row can afford to carry. PNG rather than JPEG:
//        /// the eraser leaves transparent pixels, and at this size flat UI colours compress better
//        /// as PNG anyway.
//        /// </summary>
//        public static byte[]? Create(byte[]? png)
//        {
//            if (png == null || png.Length == 0)
//                return null;

//            try
//            {
//                using MemoryStream source = new MemoryStream(png);
//                using Image image = Image.FromStream(source);

//                float scale = Math.Min((float)MaxSize / image.Width, (float)MaxSize / image.Height);
//                int width = Math.Max(1, (int)MathF.Round(image.Width * Math.Min(scale, 1f)));
//                int height = Math.Max(1, (int)MathF.Round(image.Height * Math.Min(scale, 1f)));

//                using Bitmap thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
//                using (Graphics graphics = Graphics.FromImage(thumbnail))
//                {
//                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
//                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
//                    graphics.Clear(Color.Transparent);
//                    graphics.DrawImage(image, 0, 0, width, height);
//                }

//                using MemoryStream target = new MemoryStream();
//                thumbnail.Save(target, ImageFormat.Png);
//                return target.ToArray();
//            }
//            catch (Exception ex)
//            {
//                // A template that cannot be decoded is still a usable step, so this never blocks a
//                // save. The tree just falls back to the icon.
//                Console.Error.WriteLine($"[Thumbnail] Could not build a thumbnail: {ex.Message}");
//                return null;
//            }
//        }
//    }
//}
