using Core.Helpers;

namespace Core.Tests.Helpers
{
    public sealed class TextExtractHelperTests
    {
        [Fact]
        public void The_first_group_is_kept_when_there_is_one()
        {
            TextExtractHelper.Extract("Total: 42 items", @"Total: (\d+)").ShouldBe("42");
        }

        [Fact]
        public void The_whole_match_is_kept_when_there_is_no_group()
        {
            TextExtractHelper.Extract("Total: 42 items", @"\d+").ShouldBe("42");
        }

        [Fact]
        public void No_match_keeps_nothing()
        {
            TextExtractHelper.Extract("Total: none", @"\d+").ShouldBe(string.Empty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void No_pattern_keeps_everything(string pattern)
        {
            TextExtractHelper.Extract("Total: 42", pattern).ShouldBe("Total: 42");
        }

        // A pattern the author is still typing is not a reason to fail the step.
        [Fact]
        public void A_broken_pattern_keeps_everything()
        {
            TextExtractHelper.Extract("Total: 42", "(unclosed").ShouldBe("Total: 42");
        }
    }
}
