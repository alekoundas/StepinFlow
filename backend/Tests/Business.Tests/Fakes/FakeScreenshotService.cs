using System.Drawing;

using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>A screenshot the size asked for, and nothing in it. The matcher is faked too.</summary>
    public sealed class FakeScreenshotService : IScreenshotService
    {
        public List<Rectangle> Captured { get; } = new List<Rectangle>();

        public RawImage CaptureRaw(Rectangle rect)
        {
            Captured.Add(rect);
            return new RawImage { Width = rect.Width, Height = rect.Height, Stride = rect.Width * 4, Pixels = new byte[rect.Width * rect.Height * 4] };
        }

        public byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }

        // Not a picture: one byte, so a test can tell an encoded image from none.
        public List<RawImage> Encoded { get; } = new List<RawImage>();

        public byte[] Encode(RawImage image, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            lock (Encoded)
                Encoded.Add(image);

            return [1];
        }

        public byte[] CaptureVirtualScreen(ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }

        public byte[] CaptureResolvedArea(FlowArea area, Rectangle bounds, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }

        public byte[] CaptureAppWindow(string appWindowName, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }

        public byte[] CaptureMonitor(string deviceName, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }
    }
}
