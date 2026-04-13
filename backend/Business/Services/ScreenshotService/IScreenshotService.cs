using Core.Models.Database;
using System;
using System.Drawing;

namespace Business.Services.ScreenshotService
{
    public interface IScreenshotService
    {
        public byte[] Capture(Rectangle rect);
        public byte[] CaptureVirtualScreen();
        public byte[] CaptureSearchArea(FlowSearchArea area);
    }
}
