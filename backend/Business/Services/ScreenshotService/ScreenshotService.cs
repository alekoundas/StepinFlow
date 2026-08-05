using Business.Helpers;
using Core.Enums;
using Core.Models.Business;
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
            using Bitmap bmp = CaptureGraphics(rect, ScreenshotFormatEnum.JPEG, 100);

            byte[] result = Compress(bmp, ScreenshotFormatEnum.JPEG, 100);
            return result;
        }

        public byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            byte[] result = [];
            IntPtr hMon = ScreenHelper.FindHMonitorById(deviceName);
            byte[]? monitorBytes = _windowsGraphicsCaptureService.CaptureMonitorRaw(hMon, out int monitorWidth, out int monitorHeight);

            if (monitorBytes != null)
                result = Compress(monitorBytes, monitorWidth, monitorHeight, screenshotFormat, jpegQuality);

            return result;
        }

        public byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            byte[] result = [];
            IntPtr hwnd = AppWindowHelper.FindHwndByTitle(appWindowName);
            byte[]? monitorBytes = _windowsGraphicsCaptureService.CaptureMonitorRaw(hwnd, out int width, out int height);

            if (monitorBytes != null)
                result = Compress(monitorBytes, width, height, screenshotFormat, jpegQuality);

            return result;
        }

        /// <summary>
        /// Whole desktop, every monitor, in real device pixels.
        ///
        /// A plain BitBlt of the virtual screen is not enough here: this process
        /// is DPI-unaware, so the desktop DC only covers the primary monitor's
        /// scaled coordinate space and monitors with a different scale factor
        /// come out stretched, clipped or missing entirely. Instead every monitor
        /// is captured on its own with Windows.Graphics.Capture (real pixels) and
        /// the frames are stitched using each monitor's DEVMODE position.
        /// </summary>
        public byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            byte[] stitched = CaptureVirtualScreenStitched(screenshotFormat, jpegQuality);
            if (stitched.Length > 0)
                return stitched;

            // Fallback for machines where per monitor capture is unavailable.
            Console.Error.WriteLine("[Screenshot] Per monitor capture failed, falling back to GDI");
            Rectangle rect = ScreenHelper.GetVirtualScreenBounds();
            using Bitmap bmp = CaptureGraphics(rect, screenshotFormat, jpegQuality);

            return Compress(bmp, screenshotFormat, jpegQuality);
        }


        public byte[] CaptureSearchArea(FlowSearchArea area)
        {
            byte[] result = [];

            switch (area.Type)
            {
                case FlowSearchAreaTypeEnum.CUSTOM:
                    Rectangle rect = new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height);
                    Bitmap customBmp = CaptureGraphics(rect, ScreenshotFormatEnum.JPEG, 100);
                    result = Compress(customBmp, ScreenshotFormatEnum.JPEG, 100);
                    break;
                case FlowSearchAreaTypeEnum.APPLICATION:
                    IntPtr hwnd = AppWindowHelper.FindHwndByTitle(area.AppWindowName);
                    byte[]? windowBytes = _windowsGraphicsCaptureService.CaptureWindowRaw(hwnd, out int windowWidth, out int windowHeight);


                    if (windowBytes != null)
                        result = Compress(windowBytes, windowWidth, windowHeight, ScreenshotFormatEnum.JPEG, 100);

                    break;

                case FlowSearchAreaTypeEnum.MONITOR:
                    IntPtr hMon = ScreenHelper.FindHMonitorById(area.MonitorUniqueId);
                    byte[]? monitorBytes = _windowsGraphicsCaptureService.CaptureMonitorRaw(hMon, out int monitorWidth, out int monitorHeight);

                    if (monitorBytes != null)
                        result = Compress(monitorBytes, monitorWidth, monitorHeight, ScreenshotFormatEnum.JPEG, 100);

                    break;

                default:
                    Rectangle virtualRect = ScreenHelper.GetVirtualScreenBounds();
                    Bitmap bmp = CaptureGraphics(virtualRect, ScreenshotFormatEnum.JPEG, 100);
                    result = Compress(bmp, ScreenshotFormatEnum.JPEG, 100);

                    break;
            }


            return result;
        }



        // ================================================================
        // Private helpers
        // ================================================================

        /// <summary>
        /// Capture every monitor separately and paint the frames into one bitmap
        /// laid out with the monitors' real device pixel positions.
        /// Returns an empty array when no monitor could be captured.
        /// </summary>
        private byte[] CaptureVirtualScreenStitched(ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            List<MonitorInfo> monitors = ScreenHelper.GetAllMonitors()
                .Where(x => x.HMonitor != IntPtr.Zero && x.PhysicalBounds.Width > 0 && x.PhysicalBounds.Height > 0)
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();

            if (monitors.Count == 0)
                return [];

            Rectangle virtualBounds = ScreenHelper.GetVirtualScreenBoundsPhysical();
            if (virtualBounds.Width <= 0 || virtualBounds.Height <= 0)
                return [];

            using Bitmap composite = new Bitmap(virtualBounds.Width, virtualBounds.Height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(composite);

            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            int capturedCount = 0;

            foreach (MonitorInfo monitor in monitors)
            {
                byte[]? bgra = _windowsGraphicsCaptureService.CaptureMonitorRaw(monitor.HMonitor, out int width, out int height);
                if (bgra == null || width <= 0 || height <= 0)
                {
                    Console.Error.WriteLine($"[Screenshot] Could not capture monitor {monitor.DeviceId}");
                    continue;
                }

                GCHandle pin = GCHandle.Alloc(bgra, GCHandleType.Pinned);
                try
                {
                    // Format32bppRgb: the desktop is opaque, ignore whatever the
                    // capture put in the alpha byte so the result isnt transparent.
                    using Bitmap monitorBitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppRgb, pin.AddrOfPinnedObject());

                    Rectangle destination = new Rectangle(
                        monitor.PhysicalBounds.X - virtualBounds.X,
                        monitor.PhysicalBounds.Y - virtualBounds.Y,
                        monitor.PhysicalBounds.Width,
                        monitor.PhysicalBounds.Height);

                    graphics.DrawImage(monitorBitmap, destination, 0, 0, width, height, GraphicsUnit.Pixel);
                    capturedCount++;
                }
                finally
                {
                    pin.Free();
                }
            }

            if (capturedCount == 0)
                return [];

            return Compress(composite, screenshotFormat, jpegQuality);
        }

        private Bitmap CaptureGraphics(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bmp);

            // These flags shave meaningful time off large captures
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            return bmp;
        }

        private static byte[] Compress(Bitmap bmp, ScreenshotFormatEnum format, int jpegQuality)
        {
            int pixelCount = bmp.Width * bmp.Height;
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
                    Param = { [0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality) }
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
