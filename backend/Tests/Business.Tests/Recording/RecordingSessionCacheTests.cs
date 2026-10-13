using System.Drawing;
using Business.Recording.Models.Session;
using Business.Recording.Session;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Business.Tests.Recording
{
    public sealed class RecordingSessionCacheTests
    {
        private static readonly DateTime START = new DateTime(2026, 10, 9, 12, 0, 0);

        private readonly RecordingSessionCache _cache = new RecordingSessionCache();

        private static RecordingScreenshot Screenshot(int ms)
        {
            RecordingTarget target = new RecordingTarget(Rectangle.Empty, 96, WindowHandle.None, 0);
            return new RecordingScreenshot(START.AddMilliseconds(ms), target, new RawImage());
        }

        // A screenshot finished after the press may already show what the press changed.
        [Fact]
        public void A_press_takes_the_newest_screenshot_finished_before_it()
        {
            _cache.AddRecentScreenshot(Screenshot(0));
            _cache.AddRecentScreenshot(Screenshot(250));
            _cache.AddRecentScreenshot(Screenshot(500));

            _cache.GetRecentScreenshotBefore(START.AddMilliseconds(400))!.CapturedOn.ShouldBe(START.AddMilliseconds(250));
        }

        [Fact]
        public void Only_the_last_three_screenshots_are_kept()
        {
            _cache.AddRecentScreenshot(Screenshot(0));
            _cache.AddRecentScreenshot(Screenshot(250));
            _cache.AddRecentScreenshot(Screenshot(500));
            _cache.AddRecentScreenshot(Screenshot(750));

            _cache.GetRecentScreenshotBefore(START.AddMilliseconds(100)).ShouldBeNull();
            _cache.GetRecentScreenshotBefore(START.AddMilliseconds(300))!.CapturedOn.ShouldBe(START.AddMilliseconds(250));
        }

        [Fact]
        public void Each_input_is_numbered_by_its_place_in_the_session()
        {
            RecordedInput down = new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN };
            RecordedInput up = new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_UP };

            _cache.AddInput(down).ShouldBe(0);
            _cache.AddInput(up).ShouldBe(1);

            _cache.GetInputs().Select(x => x.Index).ShouldBe([0, 1]);
        }

        [Fact]
        public void Clearing_forgets_the_whole_recording()
        {
            _cache.MainArea = new FlowArea { Name = "Browser" };
            _cache.AddInput(new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN });
            _cache.AddScreenshot(0, [1, 2, 3]);
            _cache.AddPress(new RecordedPress { Index = 0 });
            _cache.AddRecentScreenshot(Screenshot(0));

            _cache.Clear();

            _cache.MainArea.ShouldBeNull();
            _cache.GetInputs().ShouldBeEmpty();
            _cache.GetScreenshot(0).ShouldBeNull();
            _cache.GetPress(0).ShouldBeNull();
            _cache.GetRecentScreenshotBefore(START.AddSeconds(1)).ShouldBeNull();
        }
    }
}
