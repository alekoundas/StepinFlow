using Business.Executions;
using Core.Enums.Business;

namespace Business.Tests.Executions
{
    public sealed class KeyCombinationHelperTests
    {
        [Fact]
        public void Modifiers_come_before_the_key_and_ignore_case()
        {
            KeyCombinationHelper.TryParse("ctrl + SHIFT + c", out List<KeyCodeEnum> modifiers, out KeyCodeEnum key).ShouldBeTrue();

            modifiers.ShouldBe([KeyCodeEnum.LeftCtrl, KeyCodeEnum.LeftShift]);
            key.ShouldBe(KeyCodeEnum.C);
        }

        [Theory]
        [InlineData("Control+Enter", KeyCodeEnum.LeftCtrl)]
        [InlineData("Win+Tab", KeyCodeEnum.LeftMeta)]
        [InlineData("Cmd+Tab", KeyCodeEnum.LeftMeta)]
        [InlineData("Alt+F4", KeyCodeEnum.LeftAlt)]
        public void Each_modifier_has_its_other_names(string text, KeyCodeEnum modifier)
        {
            KeyCombinationHelper.TryParse(text, out List<KeyCodeEnum> modifiers, out _).ShouldBeTrue();

            modifiers.ShouldBe([modifier]);
        }

        [Fact]
        public void A_key_alone_has_no_modifiers()
        {
            KeyCombinationHelper.TryParse("Escape", out List<KeyCodeEnum> modifiers, out KeyCodeEnum key).ShouldBeTrue();

            modifiers.ShouldBeEmpty();
            key.ShouldBe(KeyCodeEnum.Escape);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("+")]
        [InlineData("Ctrl+Nope")]
        [InlineData("C+Ctrl")]
        [InlineData("Ctrl+Unknown")]
        public void Anything_else_is_refused(string text)
        {
            KeyCombinationHelper.TryParse(text, out _, out _).ShouldBeFalse();
        }

        [Fact]
        public void A_digit_is_the_number_key()
        {
            KeyCombinationHelper.TryParse("Ctrl+1", out _, out KeyCodeEnum key).ShouldBeTrue();

            key.ShouldBe(KeyCodeEnum.Num1);
        }

        [Fact]
        public void Two_keys_joined_by_a_comma_are_refused_not_combined()
        {
            KeyCombinationHelper.TryParse("Ctrl+A,B", out _, out _).ShouldBeFalse();
        }

        // What a hold puts down: a modifier on its own is a key here, where a press needs a key after it.
        [Theory]
        [InlineData("Ctrl", new[] { KeyCodeEnum.LeftCtrl })]
        [InlineData("ctrl + shift", new[] { KeyCodeEnum.LeftCtrl, KeyCodeEnum.LeftShift })]
        [InlineData("Shift+A", new[] { KeyCodeEnum.LeftShift, KeyCodeEnum.A })]
        public void Every_key_a_hold_names_is_read_in_order(string text, KeyCodeEnum[] expected)
        {
            KeyCombinationHelper.TryParseKeys(text, out List<KeyCodeEnum> keys).ShouldBeTrue();

            keys.ShouldBe(expected);
        }

        [Fact]
        public void A_hold_naming_no_key_is_refused()
        {
            KeyCombinationHelper.TryParseKeys("Ctrl+Nope", out _).ShouldBeFalse();
        }
    }
}
