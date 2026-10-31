using System.Drawing;
using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Business.Recording.Screens;
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
        private readonly FakeAppSettingService _settings = new FakeAppSettingService();
        private readonly FakeOcrService _ocr = new FakeOcrService();
        private readonly FakeAccessibleElementService _accessibility = new FakeAccessibleElementService();
        private readonly RecordingSessionService _session;

        public RecordingSessionServiceTests()
        {
            ClickedElementFinder finder = new ClickedElementFinder(new FakeElementLocator(), new FakeOcrService().Reads("Checkout"), _screenshots);
            ClickedElementGrouper grouper = new ClickedElementGrouper(new FakeOpenCvService(), _screenshots);
            RecordedScreenReader screens = new RecordedScreenReader(_ocr, _screenshots);
            _session = new RecordingSessionService(_hook, _screenshots, _settings, _broadcast, _windows, _resolver, finder, grouper, screens, _accessibility, _database, _clock);

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

        // A word OCR read on the screenshot, one engine line per line number.
        private static OcrWord Word(string text, int line)
        {
            return new OcrWord { Text = text, Bounds = new Rectangle(10, 10 + line * 30, text.Length * 8, 14), LineIndex = line };
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
        // Gestures
        // ================================================================

        // Paused, a Ctrl + click is the tester's own, and the application must get it.
        [Fact]
        public async Task The_gestures_are_held_back_while_recording_and_never_while_paused()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            _hook.HeldBack.Select(x => (x.Gesture, x.Button)).ShouldBe(
            [
                (RecordingGestureEnum.CHECK, CursorButtonTypeEnum.LEFT_BUTTON),
                (RecordingGestureEnum.SUBSTITUTE, CursorButtonTypeEnum.RIGHT_BUTTON),
            ]);

            _session.Pause();
            _hook.HeldBack.ShouldBeEmpty();

            _session.Resume();
            _hook.HeldBack.Count.ShouldBe(2);

            await _session.StopAsync(Ct);
            _hook.HeldBack.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_gesture_cleared_in_settings_holds_nothing_back()
        {
            _settings.Stored[AppSettingKeyEnum.RECORDING_HOTKEY_CHECK] = string.Empty;

            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            _hook.HeldBack.ShouldHaveSingleItem().Gesture.ShouldBe(RecordingGestureEnum.SUBSTITUTE);
        }

        [Fact]
        public async Task A_check_marked_while_recording_is_kept_without_the_ctrl_held_for_it()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.LeftCtrl, "VcLeftControl");
            _hook.Raise(new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN, PhysicalX = 500, PhysicalY = 400, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, Gesture = RecordingGestureEnum.CHECK, CreatedOn = _clock.GetLocalNow().DateTime });
            _hook.Raise(new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_UP, PhysicalX = 500, PhysicalY = 400, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, Gesture = RecordingGestureEnum.CHECK, CreatedOn = _clock.GetLocalNow().DateTime });
            RaiseKey(RecordedInputTypeEnum.KEY_UP, KeyCodeEnum.LeftCtrl, "VcLeftControl");

            IReadOnlyList<RecordedInput> recorded = await _session.StopAsync(Ct);

            recorded.Select(x => (x.Type, x.Gesture)).ShouldBe(
            [
                (RecordedInputTypeEnum.BUTTON_DOWN, RecordingGestureEnum.CHECK),
                (RecordedInputTypeEnum.BUTTON_UP, RecordingGestureEnum.CHECK),
            ]);
            recorded[0].HasScreenshot.ShouldBeTrue();
            (await WaitUntilReadyAsync()).Actions.ShouldHaveSingleItem().Kind.ShouldBe(RecordedActionKindEnum.CHECK);
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

        // Paused, the tester may be getting the next part ready, which a stage can begin at.
        [Fact]
        public async Task The_stage_key_marks_where_a_stage_begins_paused_or_not_and_is_never_recorded()
        {
            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);

            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.F8, "VcF8");
            RaiseKey(RecordedInputTypeEnum.KEY_UP, KeyCodeEnum.F8, "VcF8");
            _session.Pause();
            RaiseKey(RecordedInputTypeEnum.KEY_DOWN, KeyCodeEnum.F8, "VcF8");
            RaiseKey(RecordedInputTypeEnum.KEY_UP, KeyCodeEnum.F8, "VcF8");

            (await _session.StopAsync(Ct)).Select(x => x.Type).ShouldBe([RecordedInputTypeEnum.STAGE, RecordedInputTypeEnum.STAGE]);
            (await WaitUntilReadyAsync()).Actions.Select(x => x.Kind).ShouldBe([RecordedActionKindEnum.STAGE, RecordedActionKindEnum.STAGE]);
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
            (lastClick.StageNumber, lastClick.StageCount).ShouldBe((1, 3));
            (lastClick.StageDone, lastClick.StageTotal).ShouldBe((1, 1));

            RecordingProgressDto lastGrouped = AllProgress().Last(x => x.Stage == RecordingStageEnum.GROUPING_CLICKS);
            (lastGrouped.StageNumber, lastGrouped.StageCount).ShouldBe((2, 3));

            // The press, and the screen the recording stopped on.
            RecordingProgressDto lastRead = AllProgress().Last(x => x.Stage == RecordingStageEnum.READING_SCREENS);
            (lastRead.StageNumber, lastRead.StageCount).ShouldBe((3, 3));
            (lastRead.StageDone, lastRead.StageTotal).ShouldBe((2, 2));

            RecordingProgressDto ready = LastProgress();
            (ready.Stage, ready.StageNumber, ready.ErrorMessage).ShouldBe((RecordingStageEnum.NONE, 0, null));
        }

        [Fact]
        public async Task A_stop_reads_what_changed_on_the_screen_from_one_press_to_the_next()
        {
            _ocr.ReadsWords([Word("Login", 0)], [Word("Products", 0)]);

            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 300);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 300);
            _clock.Advance(TimeSpan.FromSeconds(2));
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 400);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 400);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            List<int> clicks = recording.Actions.Where(x => x.Kind == RecordedActionKindEnum.CLICK).Select(x => x.Index).ToList();
            recording.ScreenChanges.Count.ShouldBe(2);
            ScreenChangeDto change = recording.ScreenChanges[0];
            (change.ActionIndex, change.NextActionIndex).ShouldBe((clicks[0], clicks[1]));
            change.Appeared.ShouldBe(["Products"]);
            change.Gone.ShouldBe(["Login"]);
            change.IsNewScreen.ShouldBeTrue();
        }

        // What the last click did shows on no press of its own.
        [Fact]
        public async Task A_stop_keeps_the_screen_it_was_left_on_and_reads_what_the_last_click_did()
        {
            _ocr.ReadsWords([Word("Finish", 0)], [Word("Thank you for your order", 0)]);

            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 300);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 300);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            int click = recording.Actions.Single(x => x.Kind == RecordedActionKindEnum.CLICK).Index;
            ScreenChangeDto change = recording.ScreenChanges.ShouldHaveSingleItem();
            (change.ActionIndex, change.NextActionIndex).ShouldBe((click, null));
            change.Appeared.ShouldBe(["Thank you for your order"]);
            recording.ScreenLines.ShouldBe(["Finish", "Thank you for your order"]);
        }

        [Fact]
        public async Task A_press_is_asked_of_its_application_where_it_landed()
        {
            _accessibility.Element = new AccessibleElement
            {
                Role = "button",
                Name = "Add to cart",
                IsEnabled = false,
                Inside = [new AccessibleElement { Role = "document", Name = "Swag Labs" }],
            };

            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN, 300, 400);
            Raise(RecordedInputTypeEnum.BUTTON_UP, 300, 400);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            _accessibility.Asked.ShouldBe([new Point(300, 400)]);
            PressedElementDto pressed = recording.PressedElements.ShouldHaveSingleItem();
            pressed.ActionIndex.ShouldBe(recording.Actions.Single(x => x.Kind == RecordedActionKindEnum.CLICK).Index);
            (pressed.Role, pressed.Name, pressed.IsEnabled).ShouldBe(("button", "Add to cart", false));
            pressed.Inside.ShouldBe(["document \"Swag Labs\""]);
        }

        // An application that draws its own pixels, a game, answers with its window.
        [Fact]
        public async Task A_press_answered_only_with_a_window_says_nothing()
        {
            _accessibility.Element = new AccessibleElement { Role = "pane", Name = "Firestone" };

            await _session.StartAsync(NewFlow(withMainArea: true), OWN, Ct);
            Raise(RecordedInputTypeEnum.BUTTON_DOWN);
            Raise(RecordedInputTypeEnum.BUTTON_UP);
            await _session.StopAsync(Ct);

            RecordingResultDto recording = await WaitUntilReadyAsync();

            recording.PressedElements.ShouldBeEmpty();
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
