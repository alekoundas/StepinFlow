using Core.Models.Database;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Core.Enums;

namespace Business.Services.ScreenshotService
{
    public sealed class ScreenshotService : IScreenshotService
    {
        public byte[] Capture(Rectangle rect)
        {
            using Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bmp);

            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);

            // Lock bits for max speed + direct byte[] return
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bmp.UnlockBits(data);

            return bytes; // BGRA raw bytes 
        }

        //public Mat CaptureAsMat(Rectangle rect)
        //{
        //    byte[] bytes = Capture(rect);
        //    Mat matImage = new Mat();
        //    Cv2.
        //    matImage.

        //    return new Mat(rect.Height, rect.Width, MatType.CV_8UC4, bytes);
        //}

        // Overloads for FlowSearchArea, monitor, full screen, etc.
        //public byte[] CaptureFullScreen() => Capture(Screen.PrimaryScreen.Bounds);
        public byte[] CaptureSearchArea(FlowSearchArea area)
        {

            byte[] capture;
            switch (area.Type)
            {
                case FlowSearchAreaTypeEnum.CUSTOM:
                    capture = Capture(new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height));
                    break;
                case FlowSearchAreaTypeEnum.APPLICATION:
                    capture = Capture(new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height));
                    break;
                case FlowSearchAreaTypeEnum.MONITOR:
                    capture = Capture(new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height));
                    break;
                default:
                    capture = [];
                    break;
            }
            return capture;
        }
    }
}
