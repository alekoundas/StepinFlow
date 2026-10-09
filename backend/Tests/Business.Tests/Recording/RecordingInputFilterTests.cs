using System.Drawing;

using Business.Recording.Session;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;

namespace Business.Tests.Recording
{
    /// <summary>
    /// What of the input belongs to a recording, and which window each press landed on - decided
    /// at the hook, before the press can change what is in front.
    /// </summary>
    public sealed class RecordingInputFilterTests
    {
        private const int OWN = 42;
        private const int BROWSER = 9;

        private static readonly WindowHandle BROWSER_WINDOW = new WindowHandle(7);
        private static readonly TopLevelWindow STEPINFLOW = new TopLevelWindow(new WindowHandle(1), OWN, "StepinFlow");
        private static readonly TopLevelWindow MAIN = new TopLevelWindow(BROWSER_WINDOW, BROWSER, "Swag Labs");
        private static readonly TopLevelWindow SAVE_AS = new TopLevelWindow(new WindowHandle(8), BROWSER, "Save As");
        private static readonly TopLevelWindow NOTEPAD = new TopLevelWindow(new WindowHandle(3), 5, "Notepad");

        private readonly FakeWindowService _windows = new FakeWindowService();
        private readonly RecordingInputFilter _filter;

        public RecordingInputFilterTests()
        {
            _filter = new RecordingInputFilter(_windows)
            {
                OwnProcessId = OWN,
                Target = new RecordingTarget(new Rectangle(0, 0, 800, 600), 96, BROWSER_WINDOW, BROWSER),
            };
        }

        private static RecordedInput Press(int x = 10, int y = 10)
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN, PhysicalX = x, PhysicalY = y, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON };
        }

        private static RecordedInput Release()
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_UP, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON };
        }

        private static RecordedInput KeyDown()
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.KEY_DOWN, KeyCode = KeyCodeEnum.LeftCtrl };
        }

        private static RecordedInput KeyUp()
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.KEY_UP, KeyCode = KeyCodeEnum.LeftCtrl };
        }

        // ================================================================
        // StepinFlow's own windows
        // ================================================================

        [Fact]
        public void A_press_on_StepinFlow_is_left_out_and_its_release_with_it()
        {
            _windows.Pointed = STEPINFLOW;

            _filter.Keep(Press()).ShouldBeFalse();
            _windows.Pointed = MAIN;
            _filter.Keep(Release()).ShouldBeFalse();
        }

        [Fact]
        public void Keys_typed_into_StepinFlow_are_left_out()
        {
            _windows.InFront = STEPINFLOW;

            _filter.Keep(KeyDown()).ShouldBeFalse();
            _filter.Keep(KeyUp()).ShouldBeFalse();
        }

        // Ctrl held in the application and let go over StepinFlow: without its release, every
        // letter typed after it would read as a shortcut.
        [Fact]
        public void A_key_let_go_over_StepinFlow_is_still_let_go()
        {
            _windows.InFront = MAIN;
            _filter.Keep(KeyDown()).ShouldBeTrue();

            _windows.InFront = STEPINFLOW;
            _filter.Keep(KeyUp()).ShouldBeTrue();
        }

        [Fact]
        public void A_drag_released_over_StepinFlow_keeps_its_release()
        {
            _windows.Pointed = MAIN;
            _filter.Keep(Press()).ShouldBeTrue();

            _windows.Pointed = STEPINFLOW;
            _filter.Keep(Release()).ShouldBeTrue();
        }

        [Fact]
        public void Before_Electron_says_which_process_is_its_own_nothing_is_left_out_as_its_own()
        {
            _filter.OwnProcessId = 0;
            _windows.Pointed = STEPINFLOW;

            _filter.Keep(Press()).ShouldBeTrue();
        }

        // ================================================================
        // Pausing
        // ================================================================

        [Fact]
        public void A_paused_recording_keeps_nothing_new()
        {
            _windows.Pointed = MAIN;
            _windows.InFront = MAIN;
            _filter.IsPaused = true;

            _filter.Keep(Press()).ShouldBeFalse();
            _filter.Keep(KeyDown()).ShouldBeFalse();
        }

        [Fact]
        public void A_pause_still_lets_go_of_what_was_held_before_it()
        {
            _windows.InFront = MAIN;
            _filter.Keep(KeyDown()).ShouldBeTrue();

            _filter.IsPaused = true;

            _filter.Keep(KeyUp()).ShouldBeTrue();
        }

        // ================================================================
        // Which window
        // ================================================================

        [Fact]
        public void A_press_on_the_main_window_is_in_the_main_area()
        {
            _windows.Pointed = MAIN;
            RecordedInput press = Press();

            _filter.Keep(press);

            press.Window.ShouldBe(RecordedWindowEnum.MAIN_AREA);
            press.WindowTitle.ShouldBe("Swag Labs");
        }

        [Fact]
        public void A_press_on_a_dialog_of_the_same_application_is_told_apart()
        {
            _windows.Pointed = SAVE_AS;
            RecordedInput press = Press();

            _filter.Keep(press);

            press.Window.ShouldBe(RecordedWindowEnum.SAME_APPLICATION);
        }

        [Fact]
        public void A_press_on_anything_else_is_other()
        {
            _windows.Pointed = NOTEPAD;
            RecordedInput press = Press();

            _filter.Keep(press);

            press.Window.ShouldBe(RecordedWindowEnum.OTHER);
        }

        // A monitor or a region has no window of its own, so where the press landed decides.
        [Theory]
        [InlineData(100, 100, RecordedWindowEnum.MAIN_AREA)]
        [InlineData(900, 100, RecordedWindowEnum.OTHER)]
        public void A_main_area_with_no_window_sorts_by_where_the_press_landed(int x, int y, RecordedWindowEnum window)
        {
            _filter.Target = new RecordingTarget(new Rectangle(0, 0, 800, 600), 96, WindowHandle.None, 0);
            _windows.Pointed = NOTEPAD;
            RecordedInput press = Press(x, y);

            _filter.Keep(press);

            press.Window.ShouldBe(window);
        }

        [Fact]
        public void With_no_main_area_a_press_is_not_sorted()
        {
            _filter.Target = null;
            _windows.Pointed = NOTEPAD;
            RecordedInput press = Press();

            _filter.Keep(press);

            press.Window.ShouldBeNull();
        }

        [Fact]
        public void The_moves_of_a_drag_are_left_out()
        {
            _filter.Keep(new RecordedInput { Type = RecordedInputTypeEnum.CURSOR_DRAG }).ShouldBeFalse();
        }
    }
}
