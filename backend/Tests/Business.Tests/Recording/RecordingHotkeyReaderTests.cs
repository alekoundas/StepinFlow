using Business.Recording.Session;
using Core.Enums;
using Core.Models.Business;

namespace Business.Tests.Recording
{
    /// <summary>
    /// The recording bar's hotkeys, picked out at the hook: one pauses or stops, and none of its
    /// keys reaches the recording. Everything else does, in the order it came.
    /// </summary>
    public sealed class RecordingHotkeyReaderTests
    {
        private const string F9 = "VcF9";
        private const string F10 = "VcF10";
        private const string CTRL = "VcLeftControl";
        private const string C = "VcC";

        private readonly RecordingHotkeyReader _reader = new RecordingHotkeyReader();

        public RecordingHotkeyReaderTests()
        {
            _reader.Bind(RecordingHotkeyEnum.PAUSE, F9);
            _reader.Bind(RecordingHotkeyEnum.STOP, $"{CTRL}+{F10}");
        }

        private static RecordedInput Down(string name)
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.KEY_DOWN, KeyChar = name };
        }

        private static RecordedInput Up(string name)
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.KEY_UP, KeyChar = name };
        }

        private static RecordedInput Typed()
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.KEY_TYPED, TypedCharacter = "x" };
        }

        private static RecordedInput Press()
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN };
        }

        private IReadOnlyList<RecordedInput> Read(RecordedInput input)
        {
            return _reader.Read(input, out RecordingHotkeyEnum? _);
        }

        private RecordingHotkeyEnum? HotkeyOf(RecordedInput input)
        {
            _reader.Read(input, out RecordingHotkeyEnum? hotkey);
            return hotkey;
        }

        // ================================================================
        // A single key
        // ================================================================

        [Fact]
        public void A_hotkey_is_pressed_and_none_of_its_input_is_recorded()
        {
            IReadOnlyList<RecordedInput> released = _reader.Read(Down(F9), out RecordingHotkeyEnum? hotkey);

            hotkey.ShouldBe(RecordingHotkeyEnum.PAUSE);
            released.ShouldBeEmpty();
            Read(Typed()).ShouldBeEmpty();
            Read(Up(F9)).ShouldBeEmpty();
        }

        // Otherwise a lingering finger would pause and resume, over and over.
        [Fact]
        public void A_hotkey_held_down_is_pressed_once()
        {
            HotkeyOf(Down(F9)).ShouldBe(RecordingHotkeyEnum.PAUSE);

            HotkeyOf(Down(F9)).ShouldBeNull();
            Read(Down(F9)).ShouldBeEmpty();
        }

        [Fact]
        public void Every_other_input_is_recorded_as_it_comes()
        {
            RecordedInput down = Down(C);
            RecordedInput press = Press();

            Read(down).ShouldBe([down]);
            Read(press).ShouldBe([press]);
        }

        [Fact]
        public void An_unbound_hotkey_is_an_ordinary_key()
        {
            _reader.Bind(RecordingHotkeyEnum.PAUSE, "");
            RecordedInput down = Down(F9);

            IReadOnlyList<RecordedInput> released = _reader.Read(down, out RecordingHotkeyEnum? hotkey);

            hotkey.ShouldBeNull();
            released.ShouldBe([down]);
        }

        // ================================================================
        // A combination
        // ================================================================

        // Recorded at once, the Ctrl would be in the recording by the time F10 came.
        [Fact]
        public void A_combination_s_first_keys_wait_and_go_with_it()
        {
            Read(Down(CTRL)).ShouldBeEmpty();
            Read(Down(CTRL)).ShouldBeEmpty();

            HotkeyOf(Down(F10)).ShouldBe(RecordingHotkeyEnum.STOP);

            Read(Up(F10)).ShouldBeEmpty();
            Read(Up(CTRL)).ShouldBeEmpty();
        }

        [Fact]
        public void Keys_held_back_are_recorded_in_order_when_the_combination_does_not_follow()
        {
            RecordedInput ctrl = Down(CTRL);
            RecordedInput c = Down(C);
            RecordedInput ctrlUp = Up(CTRL);

            Read(ctrl).ShouldBeEmpty();
            Read(c).ShouldBe([ctrl, c]);
            Read(ctrlUp).ShouldBe([ctrlUp]);
        }

        // Swallowing its key-up would leave the recording holding Ctrl down for good.
        [Fact]
        public void A_key_recorded_before_the_combination_keeps_its_release()
        {
            RecordedInput ctrl = Down(CTRL);
            RecordedInput press = Press();
            RecordedInput ctrlUp = Up(CTRL);

            Read(ctrl);
            Read(press).ShouldBe([ctrl, press]);

            HotkeyOf(Down(F10)).ShouldBe(RecordingHotkeyEnum.STOP);
            Read(Up(F10)).ShouldBeEmpty();
            Read(ctrlUp).ShouldBe([ctrlUp]);
        }

        [Fact]
        public void A_combination_with_another_key_held_is_not_the_hotkey()
        {
            RecordedInput c = Down(C);
            RecordedInput ctrl = Down(CTRL);
            RecordedInput f10 = Down(F10);

            Read(c);
            Read(ctrl).ShouldBeEmpty();

            IReadOnlyList<RecordedInput> released = _reader.Read(f10, out RecordingHotkeyEnum? hotkey);

            hotkey.ShouldBeNull();
            released.ShouldBe([ctrl, f10]);
        }

        // ================================================================
        // Gestures
        // ================================================================

        private const string ALT = "VcLeftAlt";

        // Alt starts no hotkey here, so only the gesture holds it back.
        private void BindGestures()
        {
            _reader.BindGestures(
            [
                PressGesture.Parse(RecordingGestureEnum.CHECK, $"{CTRL}+LEFT_BUTTON")!,
                PressGesture.Parse(RecordingGestureEnum.SUBSTITUTE, $"{ALT}+RIGHT_BUTTON")!,
            ]);
        }

        private static RecordedInput GesturePress(RecordingGestureEnum gesture)
        {
            return new RecordedInput { Type = RecordedInputTypeEnum.BUTTON_DOWN, Gesture = gesture };
        }

        [Fact]
        public void A_gesture_s_modifier_is_left_out_with_its_press()
        {
            BindGestures();
            RecordedInput press = GesturePress(RecordingGestureEnum.SUBSTITUTE);

            Read(Down(ALT)).ShouldBeEmpty();
            Read(Down(ALT)).ShouldBeEmpty();
            Read(press).ShouldBe([press]);
            Read(Up(ALT)).ShouldBeEmpty();
        }

        // The hook decides what a press is, so one it did not hold back is a click.
        [Fact]
        public void A_modifier_held_over_a_click_goes_through_before_it()
        {
            BindGestures();
            RecordedInput alt = Down(ALT);
            RecordedInput press = Press();
            RecordedInput altUp = Up(ALT);

            Read(alt).ShouldBeEmpty();
            Read(press).ShouldBe([alt, press]);
            Read(altUp).ShouldBe([altUp]);
        }

        [Fact]
        public void A_modifier_bound_to_no_gesture_is_not_held_back()
        {
            RecordedInput alt = Down(ALT);

            Read(alt).ShouldBe([alt]);
        }
    }
}
