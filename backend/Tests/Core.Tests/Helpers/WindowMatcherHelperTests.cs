using Core.Enums;
using Core.Helpers;
using Core.Models.Business;

namespace Core.Tests.Helpers
{
    public sealed class WindowMatcherHelperTests
    {
        [Theory]
        [InlineData("Swag Labs - Chrome", "swag labs", TitleMatchModeEnum.CONTAINS, true)]
        [InlineData("Swag Labs - Chrome", "SWAG LABS - CHROME", TitleMatchModeEnum.EQUALS, true)]
        [InlineData("Swag Labs - Chrome", "Swag", TitleMatchModeEnum.STARTS_WITH, true)]
        [InlineData("Swag Labs - Chrome", "Chrome", TitleMatchModeEnum.STARTS_WITH, false)]
        [InlineData("Swag Labs - Chrome", @"^swag.*chrome$", TitleMatchModeEnum.REGEX, true)]
        [InlineData("Swag Labs - Chrome", "(unclosed", TitleMatchModeEnum.REGEX, false)]
        public void A_title_is_matched_by_its_mode_ignoring_case(string title, string pattern, TitleMatchModeEnum mode, bool matches)
        {
            WindowNameMatchHelper.IsTitleMatch(title, pattern, mode).ShouldBe(matches);
        }

        [Fact]
        public void The_process_name_must_agree_when_one_is_given()
        {
            WindowQuery query = new WindowQuery { ProcessName = "chrome", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.CONTAINS };

            WindowNameMatchHelper.Matches("Swag Labs", "CHROME", query).ShouldBeTrue();
            WindowNameMatchHelper.Matches("Swag Labs", "firefox", query).ShouldBeFalse();
        }

        [Fact]
        public void An_empty_query_matches_any_window_with_a_title()
        {
            WindowNameMatchHelper.Matches("Anything", "any", new WindowQuery()).ShouldBeTrue();
        }

        [Fact]
        public void A_window_with_no_title_never_matches()
        {
            WindowNameMatchHelper.Matches("  ", "chrome", new WindowQuery { ProcessName = "chrome" }).ShouldBeFalse();
        }
    }
}
