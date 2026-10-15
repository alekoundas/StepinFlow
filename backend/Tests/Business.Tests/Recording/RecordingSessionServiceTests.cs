using System.Drawing;
using Business.Recording.Elements;
using Business.Recording.Models.Session;
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
            ClickedElementFinder finder = new ClickedElementFinder(new FakeElementLocator(), new FakeOcrService().Reads("Checkout"), _screenshots);
            ClickedElementGrouper grouper = new ClickedElementGrouper(new FakeOpenCvService(), _screenshots);
            _session = new RecordingSessionService(_hook, _screenshots, new FakeAppSettingService(), _broadcast, _windows, _resolver, finder, grouper, _database, _clock);

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

        // A key as the hook names it, which is how a hotkey is stored.
        private void RaiseKey(RecordedInputTypeEnum type, KeyCodeEnum key, string name)
        {
            _hook.Raise(new RecordedInput
            {
                Type = type,
                KeyCode = key,
                KeyChar = name,
                CreatedOn = _clock.GetLocalNow().DateTime,
            });
        }

        private void Type(KeyCodeEnum key, string name, string character)
        {
            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, key, name);
            _hook.Raise(new RecordedInput { Type = RecordedInputTypeEnum.KEY_TYPED, KeyCode = key, TypedCharacter = character, CreatedOn = _clock.GetLocalNow().DateTime });
            RaiseKey(RecordedInputTypeEnum.KEY_UP, key, name);
        }

        // The stop key stops on a thread of its own, so the hook is never held up.
        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            while (!condition())
                await Task.Delay(10, timeout.Token);
        }

        // What every window was last told.
        private RecordingProgressDto LastProgress()
        {
            lock (_broadcast.Sent)
                return (RecordingProgressDto)_broadcast.Sent.Last(x => x.Payload is RecordingProgressDto).Payload!;
        }

        private List<RecordingProgressDto> AllProgress()
        {
            lock (_broadcast.Sent)
                return _broadcast.Sent.Select(x => x.Payload).OfType<RecordingProgressDto>().ToList();
        }

        // A stop works the recording through in the background.
        private async Task<RecordingResultDto> WaitUntilReadyAsync()
        {
            await WaitUntilAsync(() => LastProgress().State == RecordingStateEnum.READY);
            return _session.GetResult().ShouldNotBeNull();
        }

        // ================================================================
        // Starting
        // ================================================================

        // Otherwise the Stop click, and anything typed into StepinFlow, would be recorded.
        [Fact]
        public async Task Without_StepinFlow_s_own_process_nothing_is_recorded()
        {
            ResultDto<bool> started = await _session.StartAsync(NewFlow(withMainArea: true), 0, Ct);

            started.IsSuccess.ShouldBeFalse();
            _hook.IsRecording.ShouldBeFalse();
        }

        [Fact]
        public async Task A_flow_with_no_main_area_is_not_recorded()
        {
            ResultDto<bool> started = await _session.StartAsync(NewFlow(withMainArea: false), OWN, Ct);

            started.IsSuccess.ShouldBeFalse();
            _session.IsRecording.ShouldBeFalse();
            _hook.IsRecording.ShouldBeFalse();
        }

        [Fact]
        public async Task A_main_window_that_is_not_open_is_named_and_nothing_is_recorded()
        {
            _resolver.AreasByName.Clear();

            ResultDto<bool> started = await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            started.ErrorMessage.ShouldBe("No window matches \"Browser\".");
            _hook.IsRecording.ShouldBeFalse();
        }

        // ================================================================
        // A press
        // ================================================================

        [Fact]
        public async Task A_press_keeps_the_main_area_and_the_screenshot_from_before_it()
        {
            (await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct)).IsSuccess.ShouldBeTrue();
            _clock.Advance(TimeSpan.FromMilliseconds(10));

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            IReadOnlyList<RecordedInput> events = await _session.StopAsync(Ct);
            await WaitUntilReadyAsync();

            events[0].Window.ShouldBe(RecordedWindowEnum.MAIN_AREA);
            events[0].HasScreenshot.ShouldBeTrue();

            RecordedPress press = _session.GetPress(0).ShouldNotBeNull();
            (press.MainAreaBounds, press.Dpi).ShouldBe((BOUNDS, 120));
            press.Screenshot.ShouldNotBeEmpty();

            // The main area, beside what the stop works out from it.
            _screenshots.Captured.ShouldBe([BOUNDS]);
            _screenshots.Encoded.Select(x => (x.Width, x.Height)).ShouldContain((800, 600));
        }

        [Fact]
        public async Task A_press_on_StepinFlow_is_never_recorded()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            _windows.Pointed = STEPINFLOW;

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);

            (await _session.StopAsync(Ct)).ShouldBeEmpty();
            (await WaitUntilReadyAsync()).Actions.ShouldBeEmpty();
        }

        // ================================================================
        // Pausing
        // ================================================================

        [Fact]
        public async Task A_paused_recording_keeps_nothing_until_it_resumes()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

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
        // The recording bar's hotkeys
        // ================================================================

        [Fact]
        public async Task The_pause_key_pauses_and_resumes_and_is_never_recorded()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.F9, "VcF9");
            RaiseKey(RecordedInputTypeEnum.KEY_UP, KeyCodeEnum.F9, "VcF9");
            _session.IsPaused.ShouldBeTrue();
            LastProgress().State.ShouldBe(RecordingStateEnum.PAUSED);

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);

            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.F9, "VcF9");
            RaiseKey(RecordedInputTypeEnum.KEY_UP, KeyCodeEnum.F9, "VcF9");
            _session.IsPaused.ShouldBeFalse();

            (await _session.StopAsync(Ct)).Select(x => x.Type).ShouldBe([RecordedInputTypeEnum.RESUMED]);
        }

        [Fact]
        public async Task The_stop_key_stops_the_recording()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.F10, "VcF10");
            RecordingResultDto recording = await WaitUntilReadyAsync();

            _session.IsRecording.ShouldBeFalse();
            _hook.IsRecording.ShouldBeFalse();
            recording.Actions.Select(x => x.Kind).ShouldBe([RecordedActionKindEnum.CLICK]);
        }

        // ================================================================
        // The live list
        // ================================================================

        // Typed text may be a password, so the live list counts it.
        [Fact]
        public async Task A_stop_folds_the_recording_and_counts_typed_text()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            Type(KeyCodeEnum.H, "VcH", "h");
            Type(KeyCodeEnum.I, "VcI", "i");
            await _session.StopAsync(Ct);
            await WaitUntilReadyAsync();

            RecordingProgressDto progress = LastProgress();
            progress.ActionCount.ShouldBe(1);
            progress.RecentActions.Single().Line.ShouldBe("Typed 2 characters");
        }

        [Fact]
        public async Task A_discarded_recording_keeps_nothing()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            await _session.DiscardAsync(Ct);

            _session.IsRecording.ShouldBeFalse();
            _session.GetResult().ShouldBeNull();
            _session.GetPress(0).ShouldBeNull();
            LastProgress().State.ShouldBe(RecordingStateEnum.NONE);
        }

        // ================================================================
        // After the stop
        // ================================================================

        [Fact]
        public async Task A_stop_finds_what_each_click_hit_and_says_how_far_it_has_got()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            ClickedElementDto found = recording.ClickedElements.ShouldHaveSingleItem();
            found.Text.ShouldBe("Checkout");
            (found.X, found.Y).ShouldBe((380, 290));

            RecordingProgressDto lastClick = AllProgress().Last(x => x.Stage == RecordingStageEnum.FINDING_ELEMENTS);
            (lastClick.StageNumber, lastClick.StageCount).ShouldBe((1, 2));
            (lastClick.StageDone, lastClick.StageTotal).ShouldBe((1, 1));

            RecordingProgressDto lastGrouped = AllProgress().Last(x => x.Stage == RecordingStageEnum.GROUPING_CLICKS);
            (lastGrouped.StageNumber, lastGrouped.StageCount).ShouldBe((2, 2));

            RecordingProgressDto ready = LastProgress();
            (ready.Stage, ready.StageNumber, ready.ErrorMessage).ShouldBe((RecordingStageEnum.NONE, 0, null));
        }

        [Fact]
        public async Task Clicks_on_the_same_picture_in_two_places_come_back_as_one_moving_group()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 300);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 300);
            _clock.Advance(TimeSpan.FromSeconds(2));
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 400);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 400);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            List<int> clicks = recording.Actions.Where(x => x.Kind == RecordedActionKindEnum.CLICK).Select(x => x.Index).ToList();
            ClickGroupDto group = recording.ClickGroups.ShouldHaveSingleItem();
            group.ActionIndexes.ShouldBe(clicks);
            group.IsMoving.ShouldBeTrue();
        }

        // The page, the bar and the stop key can all stop it at the same moment.
        [Fact]
        public async Task A_second_stop_works_nothing_through_again()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            await _session.StopAsync(Ct);
            await WaitUntilReadyAsync();

            await _session.StopAsync(Ct);

            AllProgress().Count(x => x.State == RecordingStateEnum.READY).ShouldBe(1);
            LastProgress().State.ShouldBe(RecordingStateEnum.READY);
        }

        // Typing keeps no screenshot of its own; the screen it went into is the last click's.
        [Fact]
        public async Task Typed_text_shows_the_screen_of_the_last_click_before_it()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            Type(KeyCodeEnum.H, "VcH", "h");
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();
            RecordedActionDto click = recording.Actions.Single(x => x.Kind == RecordedActionKindEnum.CLICK);
            RecordedActionDto typing = recording.Actions.Single(x => x.Kind == RecordedActionKindEnum.TYPING);

            RecordingScreenDto screen = _session.GetScreen(typing.Index).ShouldNotBeNull();
            screen.ActionIndex.ShouldBe(click.Index);
            (screen.X, screen.Y, screen.Width, screen.Height, screen.Dpi).ShouldBe((100, 100, 800, 600, 120));
            screen.Image.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task A_review_is_kept_only_for_a_ready_recording_and_only_of_its_own_actions()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);

            ReviewedRecordingDto early = new ReviewedRecordingDto { Actions = [new ReviewedActionDto { ActionIndex = 0, Choice = ReviewChoiceEnum.LEAVE_OUT }] };
            _session.Review(early).IsSuccess.ShouldBeFalse();

            await _session.StopAsync(Ct);
            RecordingResultDto recording = await WaitUntilReadyAsync();
            int click = recording.Actions.Single(x => x.Kind == RecordedActionKindEnum.CLICK).Index;

            ReviewedRecordingDto stranger = new ReviewedRecordingDto { Actions = [new ReviewedActionDto { ActionIndex = 99, Choice = ReviewChoiceEnum.KEEP }] };
            _session.Review(stranger).IsSuccess.ShouldBeFalse();

            ReviewedRecordingDto reviewed = new ReviewedRecordingDto { Actions = [new ReviewedActionDto { ActionIndex = click, Choice = ReviewChoiceEnum.WAIT_AND_CLICK }] };
            _session.Review(reviewed).IsSuccess.ShouldBeTrue();
            _session.GetReviewed().ShouldBeSameAs(reviewed);

            await _session.DiscardAsync(Ct);
            _session.GetReviewed().ShouldBeNull();
        }

        [Fact]
        public async Task There_is_no_result_until_the_recording_is_ready()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            _session.GetResult().ShouldBeNull();
        }
    }
}
