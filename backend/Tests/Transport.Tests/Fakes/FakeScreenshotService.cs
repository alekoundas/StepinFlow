using System.Drawing;

using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Ports;

namespace Transport.Tests.Fakes
{
    /// <summary>
    /// Decoding looks the bytes up in Decoded, and encoding hands the pixels back unchanged, so a
    /// test reads exactly what was cut. Taking a screenshot fails the test.
    /// </summary>
    public sealed class FakeScreenshotService : IScreenshotService
    {
        public Dictionary<byte[], RawImage> Decoded { get; } = new Dictionary<byte[], RawImage>();

        public byte[] Encode(RawImage image, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            return image.Pixels;
        }

        public byte[] Capture(Rectangle rect, ScreenshotFormatEnum screenshotFormat, int jpegQuality)
        {
            throw new NotImplementedException();
        }

        public RawImage CaptureRaw(Rectangle rect)
        {
            throw new NotImplementedException();
        }

        public RawImage Decode(byte[] encoded)
        {
            return Decoded[encoded];
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
