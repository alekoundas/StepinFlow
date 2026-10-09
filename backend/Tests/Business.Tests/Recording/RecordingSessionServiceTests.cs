using System.Drawing;

using Business.Recording.Session;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.Extensions.Time.Testing;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// A recording against a flow's main area. The clock stands still, so the screenshot taken at
    /// start is the only one, and it is the one from before every press.
    /// </summary>
    public sealed class RecordingSessionServiceTests : IDisposable
    {
        private const int OWN = 42;
        private const int BROWSER = 9;

        private static readonly Rectangle BOUNDS = new Rectangle(100, 100, 800, 600);
        private static readonly WindowHandle BROWSER_WINDOW = new WindowHandle(7);
        private static readonly TopLevelWindow MAIN = new TopLevelWindow(BROWSER_WINDOW, BROWSER, "Swag Labs");
        private static readonly TopLevelWindow STEPINFLOW = new TopLevelWindow(new WindowHandle(1), OWN, "StepinFlow");

        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeInputRecordService _hook = new FakeInputRecordService();
        private readonly FakeScreenshotService _screenshots = new FakeScreenshotService();
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();
        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly FakeAreaPointResolver _resolver = new FakeAreaPointResolver();
        private readonly FakeTimeProvider _clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
        private readonly RecordingSessionService _session;

        public RecordingSessionServiceTests()
        {
            _session = new RecordingSessionService(_hook, _screenshots, new FakeAppSettingService(), _broadcast, _windows, new FakeScreenService(), _resolver, _database, _clock);
            _session.SetOwnProcess(OWN);

            _resolver.AreasByName["Browser"] = AreaResolution.Ok(BOUNDS, BROWSER_WINDOW, dpi: 120);
            _windows.WindowProcessId = BROWSER;
            _windows.Pointed = MAIN;
            _windows.InFront = MAIN;
        }

        public void Dispose()
        {
            _session.Dispose();
            _database.Dispose();
        }

        private int NewFlow(bool withMainArea)
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Recorded" };
            db.Flows.Add(flow);
            db.SaveChanges();

            db.FlowAreas.Add(new FlowArea { FlowId = flow.Id, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", IsMain = withMainArea });
            db.SaveChanges();

            return flow.Id;
        }

        private void Raise(RecordedInputTypeEnum type, int x = 500, int y = 400)
        {
            _hook.Raise(new RecordedInput
            {
                Type = type,
                PhysicalX = x,
                PhysicalY = y,
                CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON,
                KeyCode = KeyCodeEnum.A,
                CreatedOn = _clock.GetLocalNow().DateTime,
            });
        }

        // ================================================================
        // Starting
        // ================================================================

        [Fact]
        public async Task A_flow_with_no_main_area_is_not_recorded()
        {
            ResultDto<bool> started = await _session.StartAsync(NewFlow(withMainArea: false), Ct);

            started.IsSuccess.ShouldBeFalse();
            _session.IsRecording.ShouldBeFalse();
            _hook.IsRecording.ShouldBeFalse();
        }

        [Fact]
        public async Task A_main_window_that_is_not_open_is_named_and_nothing_is_recorded()
        {
            _resolver.AreasByName.Clear();

            ResultDto<bool> started = await _session.StartAsync(NewFlow(withMainArea: true), Ct);

            started.ErrorMessage.ShouldBe("No window matches \"Browser\".");
            _hook.IsRecording.ShouldBeFalse();
        }

        // ================================================================
        // A press
        // ================================================================

        [Fact]
        public async Task A_press_keeps_the_main_area_and_the_screenshot_from_before_it()
        {
            (await _session.StartAsync(NewFlow(withMainArea: true), Ct)).IsSuccess.ShouldBeTrue();
            _clock.Advance(TimeSpan.FromMilliseconds(10));

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            IReadOnlyList<RecordedInput> events = await _session.StopAsync(Ct);

            events[0].Window.ShouldBe(RecordedWindowEnum.MAIN_AREA);
            events[0].HasScreenshot.ShouldBeTrue();

            RecordedPress press = _session.GetPress(0).ShouldNotBeNull();
            (press.MainAreaBounds, press.Dpi).ShouldBe((BOUNDS, 120));
            press.Screenshot.ShouldNotBeEmpty();

            _screenshots.Captured.ShouldBe([BOUNDS]);
            _screenshots.Encoded.Select(x => (x.Width, x.Height)).ShouldBe([(400, 400), (800, 600)], ignoreOrder: true);
        }

        [Fact]
        public async Task A_press_on_StepinFlow_is_never_recorded()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), Ct);
            _windows.Pointed = STEPINFLOW;

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);

            (await _session.StopAsync(Ct)).ShouldBeEmpty();
            _broadcast.Sent.ShouldBeEmpty();
        }

        // ================================================================
        // Pausing
        // ================================================================

        [Fact]
        public async Task A_paused_recording_keeps_nothing_until_it_resumes()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), Ct);

            _session.Pause();
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            _session.Resume();
            Raise(RecordedInputTypeEnum.KEY_DOWN);
            Raise(RecordedInputTypeEnum.KEY_UP);

            (await _session.StopAsync(Ct)).Select(x => x.Type).ShouldBe(
            [
                RecordedInputTypeEnum.RESUMED,
                RecordedInputTypeEnum.KEY_DOWN,
                RecordedInputTypeEnum.KEY_UP,
            ]);
        }

        // ================================================================
        // Without a flow, as the wizard records
        // ================================================================

        [Fact]
        public async Task Without_a_flow_a_press_is_neither_sorted_nor_kept_against_a_main_area()
        {
            (await _session.StartAsync(null, Ct)).IsSuccess.ShouldBeTrue();

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            IReadOnlyList<RecordedInput> events = await _session.StopAsync(Ct);

            events[0].Window.ShouldBeNull();
            _session.GetPress(0).ShouldBeNull();
            _screenshots.Captured.ShouldBeEmpty();
        }
    }
}
