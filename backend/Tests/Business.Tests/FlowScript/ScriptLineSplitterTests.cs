using Business.FlowScript.Lexing;
using Business.FlowScript.Models.Text;

namespace Business.Tests.FlowScript
{
    public sealed class ScriptLineSplitterTests
    {
        [Fact]
        public void Every_line_ending_splits_and_lines_are_numbered_from_one()
        {
            IReadOnlyList<ScriptLine> lines = ScriptLineSplitter.Split("a\r\nb\rc\nd");

            lines.Select(x => x.Raw).ShouldBe(["a", "b", "c", "d"]);
            lines.Select(x => x.Number).ShouldBe([1, 2, 3, 4]);
        }

        [Fact]
        public void Trailing_whitespace_is_not_part_of_the_line()
        {
            ScriptLineSplitter.Split("Wait 800ms   \t").Single().Raw.ShouldBe("Wait 800ms");
        }

        [Theory]
        [InlineData("Wait 800ms", 0)]
        [InlineData(" Wait 800ms", 1)]
        [InlineData("  Wait 800ms", 2)]
        [InlineData("\tWait 800ms", 1)]
        [InlineData("\t Wait 800ms", 2)]
        [InlineData("", 0)]
        public void Each_space_or_tab_in_front_is_one_level(string text, int leadingSpaces)
        {
            ScriptLineSplitter.Split(text).Single().LeadingSpaces.ShouldBe(leadingSpaces);
        }
    }
}
