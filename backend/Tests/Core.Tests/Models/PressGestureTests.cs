using Core.Catalogs;
using Core.Enums;
using Core.Models.Business;

namespace Core.Tests.Models
{
    /// <summary>A recording gesture's binding, as its setting stores it.</summary>
    public sealed class PressGestureTests
    {
        [Fact]
        public void Modifiers_and_a_button_are_a_gesture()
        {
            PressGesture gesture = PressGesture.Parse(RecordingGestureEnum.CHECK, "VcLeftControl+VcLeftShift+LEFT_BUTTON").ShouldNotBeNull();

            gesture.Gesture.ShouldBe(RecordingGestureEnum.CHECK);
            gesture.Modifiers.ShouldBe([KeyCatalog.CTRL, KeyCatalog.SHIFT], ignoreOrder: true);
            gesture.Button.ShouldBe(CursorButtonTypeEnum.LEFT_BUTTON);
        }

        // A press with nothing held is a click, and a letter is no modifier the hook can see held.
        [Theory]
        [InlineData("")]
        [InlineData("LEFT_BUTTON")]
        [InlineData("VcLeftControl+VcC")]
        [InlineData("VcC+LEFT_BUTTON")]
        [InlineData("VcLeftControl+FOURTH_BUTTON")]
        public void Anything_else_is_no_gesture(string combination)
        {
            PressGesture.Parse(RecordingGestureEnum.CHECK, combination).ShouldBeNull();
        }

        // Bound with the left Ctrl, pressed with the right: a modifier is either side, as everywhere.
        [Fact]
        public void Either_side_of_the_keyboard_holds_the_modifier()
        {
            PressGesture gesture = PressGesture.Parse(RecordingGestureEnum.CHECK, "VcLeftControl+LEFT_BUTTON")!;

            gesture.Matches(new HashSet<string> { PressGesture.ModifierOf("VcRightControl")! }, CursorButtonTypeEnum.LEFT_BUTTON).ShouldBeTrue();
        }

        [Fact]
        public void Another_modifier_held_with_it_or_another_button_is_not_it()
        {
            PressGesture gesture = PressGesture.Parse(RecordingGestureEnum.CHECK, "VcLeftControl+LEFT_BUTTON")!;

            gesture.Matches(new HashSet<string> { KeyCatalog.CTRL, KeyCatalog.SHIFT }, CursorButtonTypeEnum.LEFT_BUTTON).ShouldBeFalse();
            gesture.Matches(new HashSet<string> { KeyCatalog.CTRL }, CursorButtonTypeEnum.RIGHT_BUTTON).ShouldBeFalse();
        }
    }
}
