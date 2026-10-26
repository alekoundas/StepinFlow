using Core.Enums;
using Core.Helpers;
using Core.Models.Database;

namespace Core.Tests.Helpers
{
    public sealed class FlowStepNameHelperTests
    {
        [Theory]
        [InlineData(CursorButtonActionTypeEnum.SINGLE_CLICK, CursorButtonTypeEnum.LEFT_BUTTON, "Click")]
        [InlineData(CursorButtonActionTypeEnum.SINGLE_CLICK, CursorButtonTypeEnum.RIGHT_BUTTON, "Right click")]
        [InlineData(CursorButtonActionTypeEnum.DOUBLE_CLICK, CursorButtonTypeEnum.LEFT_BUTTON, "Double click")]
        [InlineData(CursorButtonActionTypeEnum.DOUBLE_CLICK, CursorButtonTypeEnum.MIDDLE_BUTTON, "Double middle click")]
        [InlineData(CursorButtonActionTypeEnum.HOLD_CLICK, CursorButtonTypeEnum.LEFT_BUTTON, "Hold left button")]
        [InlineData(CursorButtonActionTypeEnum.RELEASE_CLICK, CursorButtonTypeEnum.RIGHT_BUTTON, "Release right button")]
        public void A_click_says_which_button_and_how(CursorButtonActionTypeEnum action, CursorButtonTypeEnum button, string expected)
        {
            FlowStep step = new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonActionType = action, CursorButtonType = button };

            FlowStepNameHelper.NameOf(step).ShouldBe(expected);
        }

        // Never from the point or step it goes to, which could be renamed under it.
        [Fact]
        public void A_move_and_a_drag_say_nothing_of_where_they_go()
        {
            FlowStep find = new FlowStep { Name = "Find login" };
            FlowPoint corner = new FlowPoint { Name = "Corner" };

            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReference = find }).ShouldBe("Move cursor");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowStepReference = find, FlowPointEnd = corner }).ShouldBe("Drag");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON }).ShouldBe("Right drag");
        }

        [Fact]
        public void A_scroll_and_a_wait_say_how_much()
        {
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = CursorScrollDirectionTypeEnum.UP, LoopCount = 3 }).ShouldBe("Scroll up 3");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 90_000 }).ShouldBe("Wait 1m 30s");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 500, WaitForMillisecondsMax = 2000 }).ShouldBe("Wait 500ms to 2s");
        }

        // What was typed is the user's: cut short, and kept clear of the brackets names are quoted in.
        [Fact]
        public void Typing_says_what_is_typed_and_keys_what_is_pressed()
        {
            FlowStep typing = new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "hello <[ there ]> and a great deal more besides" };
            FlowStep hold = new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardKeyActionType = KeyboardKeyActionTypeEnum.HOLD, KeyboardInputText = "Ctrl" };

            FlowStepNameHelper.NameOf(typing).ShouldBe("Type hello there and a great deal m...");
            FlowStepNameHelper.NameOf(hold).ShouldBe("Hold Ctrl");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT }).ShouldBe("Type text");
        }

        [Fact]
        public void A_window_step_says_what_it_does_and_not_which_window_since_its_area_can_be_renamed()
        {
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, FlowArea = new FlowArea { Name = "Browser" } }).ShouldBe("Focus window");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, WindowWidth = 1280, WindowHeight = 720 }).ShouldBe("Resize window to 1280x720");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE, FlowPoint = new FlowPoint { Name = "Left half" } }).ShouldBe("Move window");
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = SystemActionTypeEnum.MONITOR_OFF }).ShouldBe("Monitor off");
        }

        [Fact]
        public void Other_steps_keep_the_name_they_were_given()
        {
            FlowStepNameHelper.IsNamedBySettings(FlowStepTypeEnum.SEARCH_IMAGE).ShouldBeFalse();
            FlowStepNameHelper.NameOf(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find login" }).ShouldBe("Find login");
        }
    }
}
