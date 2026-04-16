using Core.Models.Database;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Core.Enums;
using Business.Helpers;
using Core.Models.Business;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace Business.Services.ScreenshotService
{
    public sealed class ScreenshotService : IScreenshotService
    {
        private readonly IDirect3DDevice _d3dDevice;

        public ScreenshotService()
        {
            // Create a D3D device for the capture pool (required by WGC)
            _d3dDevice = Direct3D11Helper.CreateDevice();
        }

        public byte[] Capture(Rectangle rect, bool isJPEG, int jpegQuality)
        {
            using Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(bmp);

            graphics.CopyFromScreen(rect.X, rect.Y, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);

            int estimatedSize = rect.Width * rect.Height / 4; // rough guess (e.g. ~4 bytes per pixel compressed)
            using var ms = new MemoryStream(Math.Max(4 * 1024 * 1024, estimatedSize));

            // Lock bits for max speed + direct byte[] return
            //var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            //var bytes = new byte[data.Stride * data.Height];
            //Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            //bmp.UnlockBits(data);

            //return bytes; // BGRA raw bytes 

            if (isJPEG) //JPEG
            {
                ImageCodecInfo jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);

                bmp.Save(ms, jpegEncoder, encoderParams);
            }
            else // PNG 
            {
                bmp.Save(ms, ImageFormat.Png);
                // JPEG with configurable quality (1-100)
            }

            return ms.ToArray();
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == format.Guid)
                   ?? throw new InvalidOperationException($"Encoder for {format} not found");
        }


        public byte[] CaptureVirtualScreen()
        {
            Rectangle rect = ScreenHelper.GetVirtualScreenBounds();
            return Capture(rect);
        }


        public byte[] CaptureSearchArea(FlowSearchArea area)
        {
            Rectangle rect;
            switch (area.Type)
            {
                case FlowSearchAreaTypeEnum.CUSTOM:
                    rect = new Rectangle(area.LocationX, area.LocationY, area.Width, area.Height);
                    break;
                case FlowSearchAreaTypeEnum.APPLICATION:
                    rect = AppWindowHelper.GetApplicationWindowBounds(area.AppWindowName);
                    break;
                case FlowSearchAreaTypeEnum.MONITOR:
                    IEnumerable<MonitorInfo> monitors = ScreenHelper.GetAllMonitors();
                    MonitorInfo? monitor = monitors.FirstOrDefault(x => x.UniqueId == area.MonitorUniqueId);

                    if (monitor == null)
                        rect = ScreenHelper.GetVirtualScreenBounds();
                    else
                        rect = monitor.Bounds;

                    break;
                default:
                    rect = ScreenHelper.GetVirtualScreenBounds();
                    break;
            }

            return Capture(rect);
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
