using Business.Recording.Actions;
using Core.Enums;
using Core.Enums.Business;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Tests.Recording
{
    /// <summary>
    /// What raw input folds into: the actions a person would say they did.
    /// </summary>
    public sealed class RecordingActionBuilderTests
    {
        private static readonly DateTime START = new DateTime(2026, 10, 9, 12, 0, 0);

        private readonly List<RecordedInput> _events = new List<RecordedInput>();

        private List<RecordedActionDto> Build()
        {
            return RecordingActionBuilder.Build(_events, TimeProvider.System);
        }

        private RecordingActionBuilderTests Press(int ms, int x, int y, CursorButtonTypeEnum button = CursorButtonTypeEnum.LEFT_BUTTON)
        {
            _events.Add(new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN, PhysicalX = x, PhysicalY = y, CursorButtonType = button, CreatedOn = START.AddMilliseconds(ms), Index = _events.Count });
            return this;
        }

        private RecordingActionBuilderTests Release(int ms, int x, int y, CursorButtonTypeEnum button = CursorButtonTypeEnum.LEFT_BUTTON)
        {
            _events.Add(new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_UP, PhysicalX = x, PhysicalY = y, CursorButtonType = button, CreatedOn = START.AddMilliseconds(ms), Index = _events.Count });
            return this;
        }

        private RecordingActionBuilderTests Click(int ms, int x, int y)
        {
            return Press(ms, x, y).Release(ms + 50, x, y);
        }

        private RecordingActionBuilderTests Scroll(int ms, CursorScrollDirectionTypeEnum direction, int amount = 1)
        {
            _events.Add(new RecordedInput { Type = RecordedInputTypeEnum.CURSOR_SCROLL, PhysicalX = 10, PhysicalY = 20, ScrollDirection = direction, ScrollAmount = amount, CreatedOn = START.AddMilliseconds(ms) });
            return this;
        }

        private RecordingActionBuilderTests Down(int ms, KeyCodeEnum key)
        {
            _events.Add(new RecordedInput { Type = RecordedInputTypeEnum.KEY_DOWN, KeyCode = key, CreatedOn = START.AddMilliseconds(ms) });
            return this;
        }

        private RecordingActionBuilderTests Up(int ms, KeyCodeEnum key)
        {
            _events.Add(new RecordedInput { Type = RecordedInputTypeEnum.KEY_UP, KeyCode = key, CreatedOn = START.AddMilliseconds(ms) });
            return this;
        }

        private RecordingActionBuilderTests Key(int ms, KeyCodeEnum key)
        {
            return Down(ms, key).Up(ms + 30, key);
        }

        // ================================================================
        // Clicks and drags
        // ================================================================

        [Fact]
        public void A_press_and_release_in_place_is_one_click()
        {
            Click(0, 100, 200).Key(100, KeyCodeEnum.A);

            RecordedActionDto click = Build()[0];

            click.Kind.ShouldBe(RecordedActionKindEnum.CLICK);
            (click.LocationX, click.LocationY).ShouldBe((100, 200));
            click.CursorButtonType.ShouldBe(CursorButtonTypeEnum.LEFT_BUTTON);
            click.CursorButtonActionType.ShouldBe(CursorButtonActionTypeEnum.SINGLE_CLICK);
            click.Summary.ShouldBe("Clicked at 100, 200");
        }

        [Fact]
        public void A_release_within_five_pixels_is_still_a_click()
        {
            Press(0, 100, 200).Release(50, 105, 195).Key(100, KeyCodeEnum.A);

            Build()[0].Kind.ShouldBe(RecordedActionKindEnum.CLICK);
        }

        [Fact]
        public void A_release_further_away_is_a_drag_from_the_press_to_the_release()
        {
            Press(0, 100, 200).Release(300, 160, 200).Key(400, KeyCodeEnum.A);

            RecordedActionDto drag = Build()[0];

            drag.Kind.ShouldBe(RecordedActionKindEnum.DRAG);
            (drag.LocationX, drag.LocationY, drag.LocationEndX, drag.LocationEndY).ShouldBe((100, 200, 160, 200));
        }

        [Fact]
        public void Two_presses_close_together_at_one_spot_are_a_double_click()
        {
            Click(0, 100, 200).Click(150, 103, 202).Key(400, KeyCodeEnum.A);

            List<RecordedActionDto> actions = Build();

            actions[0].CursorButtonActionType.ShouldBe(CursorButtonActionTypeEnum.DOUBLE_CLICK);
            actions[1].Kind.ShouldBe(RecordedActionKindEnum.TYPING);
        }

        [Fact]
        public void Two_presses_more_than_half_a_second_apart_are_two_clicks()
        {
            Click(0, 100, 200).Click(520, 100, 200).Key(700, KeyCodeEnum.A);

            Build().Count(x => x.Kind == RecordedActionKindEnum.CLICK).ShouldBe(2);
        }

        [Fact]
        public void Two_presses_at_different_spots_are_two_clicks()
        {
            Click(0, 100, 200).Click(150, 140, 200).Key(400, KeyCodeEnum.A);

            Build().Count(x => x.Kind == RecordedActionKindEnum.CLICK).ShouldBe(2);
        }

        [Fact]
        public void A_click_names_the_screenshot_taken_at_its_press()
        {
            Click(0, 100, 200).Key(100, KeyCodeEnum.A);
            _events[0].HasScreenshot = true;

            Build()[0].ScreenshotIndex.ShouldBe(0);
        }

        // The click that stopped the recording is part of stopping it, not part of the task.
        [Fact]
        public void The_last_click_is_left_out()
        {
            Key(0, KeyCodeEnum.A).Click(100, 100, 200);

            Build().ShouldAllBe(x => x.Kind != RecordedActionKindEnum.CLICK);
        }

        // ================================================================
        // Scrolling
        // ================================================================

        [Fact]
        public void Notches_in_one_direction_are_one_scroll()
        {
            Scroll(0, CursorScrollDirectionTypeEnum.DOWN).Scroll(40, CursorScrollDirectionTypeEnum.DOWN, 2).Scroll(80, CursorScrollDirectionTypeEnum.UP);

            List<RecordedActionDto> actions = Build();

            actions.Select(x => (x.ScrollDirection, x.ScrollAmount)).ShouldBe(
            [
                (CursorScrollDirectionTypeEnum.DOWN, 3),
                (CursorScrollDirectionTypeEnum.UP, 1),
            ]);
        }

        // ================================================================
        // Keys
        // ================================================================

        [Fact]
        public void Letters_typed_in_a_row_are_one_entry_and_shift_raises_them()
        {
            Down(0, KeyCodeEnum.LeftShift).Key(10, KeyCodeEnum.H).Up(50, KeyCodeEnum.LeftShift).Key(100, KeyCodeEnum.I).Key(200, KeyCodeEnum.Space).Key(300, KeyCodeEnum.Num1);

            RecordedActionDto typed = Build().ShouldHaveSingleItem();

            typed.Kind.ShouldBe(RecordedActionKindEnum.TYPING);
            typed.Text.ShouldBe("Hi 1");
        }

        // A modifier is often let go a moment before the letter: read at the release, this would
        // find no Ctrl and type a c.
        [Fact]
        public void Ctrl_and_a_letter_is_a_shortcut_even_when_ctrl_comes_up_first()
        {
            Down(0, KeyCodeEnum.LeftCtrl).Down(50, KeyCodeEnum.C).Up(80, KeyCodeEnum.LeftCtrl).Up(100, KeyCodeEnum.C);

            RecordedActionDto shortcut = Build().ShouldHaveSingleItem();

            shortcut.Kind.ShouldBe(RecordedActionKindEnum.KEY_COMBINATION);
            shortcut.Text.ShouldBe("Ctrl+C");
        }

        [Fact]
        public void A_shortcut_is_named_in_the_order_it_is_written()
        {
            Down(0, KeyCodeEnum.LeftShift).Down(10, KeyCodeEnum.RightCtrl).Key(50, KeyCodeEnum.S);

            Build().ShouldHaveSingleItem().Text.ShouldBe("Ctrl+Shift+S");
        }

        [Fact]
        public void Enter_ends_the_typing_and_stands_on_its_own()
        {
            Key(0, KeyCodeEnum.A).Key(100, KeyCodeEnum.B).Key(200, KeyCodeEnum.Enter).Key(300, KeyCodeEnum.C);

            Build().Select(x => (x.Kind, x.Text)).ShouldBe(
            [
                (RecordedActionKindEnum.TYPING, "ab"),
                (RecordedActionKindEnum.KEY_COMBINATION, "Enter"),
                (RecordedActionKindEnum.TYPING, "c"),
            ]);
        }

        // Auto-repeat arrives as more key-downs with no key-up between them.
        [Fact]
        public void A_held_key_is_one_press_that_counts_its_repeats()
        {
            Down(0, KeyCodeEnum.Backspace).Down(500, KeyCodeEnum.Backspace).Down(530, KeyCodeEnum.Backspace).Up(1200, KeyCodeEnum.Backspace);

            RecordedActionDto held = Build().ShouldHaveSingleItem();

            (held.RepeatCount, held.HoldMilliseconds).ShouldBe((2, 1200));
            held.Summary.ShouldBe("Pressed Backspace, held 1.2s");
        }

        [Fact]
        public void A_modifier_on_its_own_records_nothing()
        {
            Key(0, KeyCodeEnum.LeftShift).Key(100, KeyCodeEnum.LeftCtrl);

            Build().ShouldBeEmpty();
        }

        // ================================================================
        // Pauses and order
        // ================================================================

        [Fact]
        public void A_gap_of_more_than_550ms_is_a_pause_and_a_shorter_one_is_not()
        {
            Key(0, KeyCodeEnum.Enter).Key(400, KeyCodeEnum.Tab).Key(1430, KeyCodeEnum.Escape);

            List<RecordedActionDto> actions = Build();

            actions.Select(x => x.Kind).ShouldBe(
            [
                RecordedActionKindEnum.KEY_COMBINATION,
                RecordedActionKindEnum.KEY_COMBINATION,
                RecordedActionKindEnum.PAUSE,
                RecordedActionKindEnum.KEY_COMBINATION,
            ]);
            actions[2].PauseMilliseconds.ShouldBe(1000);
        }

        [Fact]
        public void Actions_are_numbered_in_the_order_they_happened()
        {
            Key(0, KeyCodeEnum.Enter).Key(1000, KeyCodeEnum.Tab);

            Build().Select(x => x.Index).ShouldBe([0, 1, 2]);
        }
    }
}
