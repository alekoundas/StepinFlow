using Core.Helpers;

namespace Core.Tests.Helpers
{
    /// <summary>
    /// The one place a regular expression runs. Most patterns are typed by a flow author, so what is
    /// worth pinning is what happens to a pattern that is wrong rather than what happens to a good
    /// one: nothing may throw at the caller, and nothing may run for ever.
    /// </summary>
    public sealed class RegexHelperTests
    {
        // A pattern that backtracks catastrophically: every way of splitting the a's between the two
        // quantifiers is tried, the final b fails every one of them, and the timeout is the only way
        // out. The one test using it costs that timeout, and is the only thing proving a flow
        // author's runaway pattern cannot hang an execution.
        private const string _catastrophic = "(a+)+$";
        private const string _nearlyMatches = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaab";

        // ================================================================
        // Extract
        // ================================================================

        [Fact]
        public void The_first_group_is_kept_when_there_is_one()
        {
            RegexHelper.Extract("Total: 42 items", @"Total: (\d+)").ShouldBe("42");
        }

        [Fact]
        public void The_whole_match_is_kept_when_there_is_no_group()
        {
            RegexHelper.Extract("Total: 42 items", @"\d+").ShouldBe("42");
        }

        [Fact]
        public void No_match_keeps_nothing()
        {
            RegexHelper.Extract("Total: none", @"\d+").ShouldBe(string.Empty);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void No_pattern_keeps_everything(string pattern)
        {
            RegexHelper.Extract("Total: 42", pattern).ShouldBe("Total: 42");
        }

        // A pattern the author is still typing is not a reason to fail the step.
        [Fact]
        public void A_broken_pattern_keeps_everything()
        {
            RegexHelper.Extract("Total: 42", "(unclosed").ShouldBe("Total: 42");
        }

        // Giving up and not matching are told apart by what comes back: a pattern that found nothing
        // keeps nothing, and one that ran out of time keeps the text. Which is why the timeout is
        // proven here rather than against IsMatch, where both answers are false.
        [Fact]
        public void A_pattern_that_gives_up_keeps_everything()
        {
            RegexHelper.Extract(_nearlyMatches, _catastrophic).ShouldBe(_nearlyMatches);
        }

        // ================================================================
        // IsMatch
        // ================================================================

        [Theory]
        [InlineData("Swag Labs - Chrome", @"^swag.*chrome$", true)]
        [InlineData("Swag Labs - Chrome", @"^SWAG", true)]
        [InlineData("Swag Labs - Chrome", "firefox", false)]
        public void A_match_ignores_case(string text, string pattern, bool matches)
        {
            RegexHelper.IsMatch(text, pattern).ShouldBe(matches);
        }

        // The question was whether this text matches, and neither answer is yes.
        [Fact]
        public void A_broken_pattern_matches_nothing()
        {
            RegexHelper.IsMatch("anything", "(unclosed").ShouldBeFalse();
        }

        // ================================================================
        // PatternError
        // ================================================================

        [Fact]
        public void A_pattern_that_compiles_has_no_error()
        {
            RegexHelper.PatternError(@"^swag.*chrome$").ShouldBeEmpty();
        }

        [Fact]
        public void An_empty_pattern_has_no_error()
        {
            RegexHelper.PatternError(string.Empty).ShouldBeEmpty();
        }

        // The form shows this, so it has to say something about the pattern rather than just fail.
        [Fact]
        public void A_broken_pattern_says_what_is_wrong_with_it()
        {
            string error = RegexHelper.PatternError("(unclosed");

            error.ShouldNotBeEmpty();
            error.ShouldContain("(unclosed");
        }

        // ================================================================
        // Captures
        // ================================================================

        [Fact]
        public void Every_match_gives_up_its_group_in_the_order_they_appear()
        {
            RegexHelper.Captures("a=1, b=2, c=3", @"(\w)=\d", 1).ShouldBe(["a", "b", "c"]);
        }

        [Fact]
        public void Group_zero_is_the_whole_match()
        {
            RegexHelper.Captures("a=1, b=2", @"\w=\d", 0).ShouldBe(["a=1", "b=2"]);
        }

        // Two alternatives, so exactly one group is filled in by each match.
        [Fact]
        public void A_group_that_did_not_take_part_is_left_out()
        {
            RegexHelper.Captures("one two one", "(one)|(two)", 2).ShouldBe(["two"]);
        }

        [Fact]
        public void No_match_captures_nothing()
        {
            RegexHelper.Captures("nothing here", @"(\d+)", 1).ShouldBeEmpty();
        }

        // A pattern written in the repository rather than typed, so a broken one is a bug.
        [Fact]
        public void A_broken_pattern_is_a_bug_when_the_code_wrote_it()
        {
            Should.Throw<ArgumentException>(() => RegexHelper.Captures("text", "(unclosed", 1));
        }

        // ================================================================
        // Replace
        // ================================================================

        [Fact]
        public void Each_match_is_replaced_by_what_its_groups_make()
        {
            string replaced = RegexHelper.Replace("a=1, b=2", @"(\w)=(\d)", groups => $"{groups[2]}={groups[1]}");

            replaced.ShouldBe("1=a, 2=b");
        }

        [Fact]
        public void The_whole_match_is_group_zero()
        {
            RegexHelper.Replace("a=1", @"\w=\d", groups => $"[{groups[0]}]").ShouldBe("[a=1]");
        }

        [Fact]
        public void A_group_that_did_not_take_part_is_empty_rather_than_missing()
        {
            RegexHelper.Replace("one", "(one)|(two)", groups => $"1:{groups[1]} 2:{groups[2]}").ShouldBe("1:one 2:");
        }

        [Fact]
        public void Text_with_nothing_to_replace_comes_back_as_it_was()
        {
            RegexHelper.Replace("nothing here", @"(\d+)", groups => "!").ShouldBe("nothing here");
        }

        [Fact]
        public void A_broken_pattern_is_a_bug_when_the_code_wrote_it_here_too()
        {
            Should.Throw<ArgumentException>(() => RegexHelper.Replace("text", "(unclosed", groups => "!"));
        }
    }
}
