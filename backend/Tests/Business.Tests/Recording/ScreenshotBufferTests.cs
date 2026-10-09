using System.Drawing;

using Business.Recording.Session;
using Core.Models.Business;

namespace Business.Tests.Recording
{
    public sealed class ScreenshotBufferTests
    {
        private static readonly DateTime START = new DateTime(2026, 10, 9, 12, 0, 0);

        private static RecordingFrame Frame(int ms)
        {
            return new RecordingFrame(START.AddMilliseconds(ms), new RecordingTarget(Rectangle.Empty, 96, WindowHandle.None, 0), new RawImage());
        }

        // A screenshot finished after the press may already show what the press changed.
        [Fact]
        public void A_press_takes_the_newest_screenshot_finished_before_it()
        {
            ScreenshotBuffer buffer = new ScreenshotBuffer(3);
            buffer.Add(Frame(0));
            buffer.Add(Frame(250));
            buffer.Add(Frame(500));

            buffer.LatestBefore(START.AddMilliseconds(400))!.CapturedOn.ShouldBe(START.AddMilliseconds(250));
        }

        [Fact]
        public void Only_the_last_few_are_kept()
        {
            ScreenshotBuffer buffer = new ScreenshotBuffer(2);
            buffer.Add(Frame(0));
            buffer.Add(Frame(250));
            buffer.Add(Frame(500));

            buffer.LatestBefore(START.AddMilliseconds(100)).ShouldBeNull();
        }
    }
}
