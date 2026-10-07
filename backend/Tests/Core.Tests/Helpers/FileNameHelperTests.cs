using Core.Helpers;

namespace Core.Tests.Helpers
{
    public sealed class FileNameHelperTests
    {
        [Theory]
        [InlineData("Login and add to cart")]
        [InlineData("Desktop nav - v2")]
        [InlineData("console")]
        [InlineData("COM10")]
        public void A_name_every_system_accepts_is_valid(string name)
        {
            FileNameHelper.Validate(name).ShouldBeNull();
        }

        // Linux accepts every one of these, which is why the list is fixed rather than the machine's.
        [Theory]
        [InlineData("")]
        [InlineData("Login: smoke")]
        [InlineData("Is it up?")]
        [InlineData("a\\b")]
        [InlineData("tab\there")]
        [InlineData("Login.")]
        [InlineData("Login ")]
        [InlineData("CON")]
        [InlineData("nul.txt")]
        [InlineData("Lpt1.tar.gz")]
        public void A_name_one_system_refuses_is_invalid(string name)
        {
            FileNameHelper.Validate(name).ShouldNotBeNull();
        }

        [Theory]
        [InlineData("Login and add to cart", "Login and add to cart")]
        [InlineData("a/b", "a b")]
        [InlineData("3 Is it up?", "3 Is it up")]
        [InlineData("  Find the button  ", "Find the button")]
        [InlineData("...hidden.", "hidden")]
        [InlineData("<[ quoted ]>", "[ quoted ]")]
        public void Clean_makes_each_refused_character_a_space_and_trims_the_ends(string name, string expected)
        {
            FileNameHelper.Clean(name, "flow").ShouldBe(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("?*:")]
        [InlineData("...")]
        public void Clean_falls_back_when_nothing_is_left(string name)
        {
            FileNameHelper.Clean(name, "template").ShouldBe("template");
        }

        [Theory]
        [InlineData("con", "template con")]
        [InlineData("NUL.x", "template NUL.x")]
        public void Clean_puts_the_fallback_in_front_of_a_device_name(string name, string expected)
        {
            string cleaned = FileNameHelper.Clean(name, "template");

            cleaned.ShouldBe(expected);
            FileNameHelper.Validate(cleaned).ShouldBeNull();
        }
    }
}
