using Core.Helpers;

namespace Core.Tests.Helpers
{
    public sealed class CommandLineHelperTests
    {
        [Theory]
        [InlineData("notepad.exe", "notepad.exe", "")]
        [InlineData("chrome.exe https://www.saucedemo.com", "chrome.exe", "https://www.saucedemo.com")]
        [InlineData("  chrome.exe   --incognito  https://a.b  ", "chrome.exe", "--incognito  https://a.b")]
        [InlineData("\"C:\\Program Files\\App\\app.exe\"", "C:\\Program Files\\App\\app.exe", "")]
        [InlineData("\"C:\\Program Files\\App\\app.exe\" --profile \"my profile\"", "C:\\Program Files\\App\\app.exe", "--profile \"my profile\"")]
        public void The_program_comes_first_and_the_rest_is_its_arguments(string commandLine, string program, string arguments)
        {
            CommandLineHelper.SplitProgram(commandLine, out string rest).ShouldBe(program);
            rest.ShouldBe(arguments);
        }

        // Half typed, rather than a quote that swallows the rest.
        [Fact]
        public void A_quote_that_never_closes_is_the_whole_program()
        {
            CommandLineHelper.SplitProgram("\"C:\\Program Files\\App", out string rest).ShouldBe("C:\\Program Files\\App");
            rest.ShouldBeEmpty();
        }
    }
}
