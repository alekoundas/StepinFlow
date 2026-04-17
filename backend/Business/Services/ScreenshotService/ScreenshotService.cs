using Business.Helpers;
using Core.Enums;
using Core.Models.Database;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Business.Services.ScreenshotService
{
    public sealed class ScreenshotService : IScreenshotService
    {
        private readonly IWindowsGraphicsCaptureService _windowsGraphicsCaptureService;

        public ScreenshotService(IWindowsGraphicsCaptureService windowsGraphicsCaptureService)
        {
            _windowsGraphicsCaptureService = windowsGraphicsCaptureService;
        }


        // ================================================================
        // Public methods
        // ================================================================
        public byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            Bitmap bmp = CaptureGraphics(rect, ScreenshotFormatEnum.JPEG, 100);

            byte[] result = Compress(bmp, rect.Width, rect.Height, ScreenshotFormatEnum.JPEG, 100);
            return result;
        }


        public byte[] CaptureVirtualScreen()
        {
            Rectangle rect = ScreenHelper.GetVirtualScreenBounds();
            Bitmap bmp = CaptureGraphics(rect, ScreenshotFormatEnum.JPEG, 100);

            byte[] result = Compress(bmp, rect.Width, rect.Height, ScreenshotFormatEnum.JPEG, 100);
            return result;
        }


        public byte[] CaptureSearchArea(FlowSearchArea area)
        {
            byte[] result = [];

            switch (area.Type)
            {
                case FlowSearchAreaTypeEnum.CUSTOM:
                    Rectangle rect = new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height);
                    Bitmap customBmp = CaptureGraphics(rect, ScreenshotFormatEnum.JPEG, 100);
                    result = Compress(customBmp, area.Width, area.Height, ScreenshotFormatEnum.JPEG, 100);
                    break;
                case FlowSearchAreaTypeEnum.APPLICATION:
                    IntPtr hwnd = AppWindowHelper.FindHwndByTitle(area.AppWindowName);
                    byte[]? windowBytes = _windowsGraphicsCaptureService.CaptureWindowRaw(hwnd, out int windowWidth, out int windowHeight);


                    if (windowBytes != null)
                        result = Compress(windowBytes, windowWidth, windowHeight, ScreenshotFormatEnum.JPEG, 100);

                    break;

                case FlowSearchAreaTypeEnum.MONITOR:
                    IntPtr hMon = ScreenHelper.FindHMonitorById(area.AppWindowName);
                    byte[]? monitorBytes = _windowsGraphicsCaptureService.CaptureMonitorRaw(hMon, out int monitorWidth, out int monitorHeight);

                    if (monitorBytes != null)
                        result = Compress(monitorBytes, monitorWidth, monitorHeight, ScreenshotFormatEnum.JPEG, 100);

                    break;

                default:
                    Rectangle virtualRect = ScreenHelper.GetVirtualScreenBounds();
                    Bitmap bmp = CaptureGraphics(virtualRect, ScreenshotFormatEnum.JPEG, 100);
                    result = Compress(bmp, virtualRect.Width, virtualRect.Height, ScreenshotFormatEnum.JPEG, 100);

                    break;
            }


            return result;
        }



        // ================================================================
        // Private helpers 
        // ================================================================

        private Bitmap CaptureGraphics(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            using Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bmp);

            // These flags shave meaningful time off large captures
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            return bmp;
        }

        private static byte[] Compress(Bitmap bmp, int width, int height, ScreenshotFormatEnum format, int jpegQuality)
        {
            int pixelCount = width * height;
            int initialCapacity = format == ScreenshotFormatEnum.JPEG
                ? pixelCount / 4   // JPEG: ~2 bits/pixel → /4 bytes is generous
                : pixelCount / 2;  // PNG:  harder to predict, raw/2 is a safe over-estimate

            using MemoryStream ms = new MemoryStream(initialCapacity);



            if (format == ScreenshotFormatEnum.PNG)
            {
                bmp.Save(ms, ImageFormat.Png);
            }
            else
            {
                ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters ep = new EncoderParameters(1)
                {
                    Param = { [0] = new EncoderParameter(Encoder.Quality, jpegQuality) }
                };
                bmp.Save(ms, codec, ep);
            }

            return ms.ToArray();
        }

        private static byte[] Compress(byte[] bgra, int width, int height, ScreenshotFormatEnum format, int jpegQuality)
        {
            var pin = GCHandle.Alloc(bgra, GCHandleType.Pinned);
            try
            {
                using Bitmap bmp = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, pin.AddrOfPinnedObject());
                using MemoryStream ms = new MemoryStream(bgra.Length / 3); // good initial size

                if (format == ScreenshotFormatEnum.PNG)
                {
                    bmp.Save(ms, ImageFormat.Png);
                }
                else
                {
                    ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    EncoderParameters ep = new EncoderParameters(1)
                    {
                        Param = { [0] = new EncoderParameter(Encoder.Quality, jpegQuality) }
                    };
                    bmp.Save(ms, codec, ep);
                }
                return ms.ToArray();
            }
            finally
            {
                pin.Free();
            }
        }



        //public Mat CaptureAsMat(Rectangle rect)
        //{
        //    byte[] bytes = Capture(rect);
        //    Mat matImage = new Mat();
        //    Cv2.
        //    matImage.

        //    return new Mat(rect.Height, rect.Width, MatType.CV_8UC4, bytes);
        //}
    }
}
