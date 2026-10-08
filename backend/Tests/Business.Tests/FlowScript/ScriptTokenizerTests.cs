using Business.FlowScript.Catalogs;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models.Text;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A line cut into tokens: keywords joined from the catalog, quoted text kept whole, and
    /// anything else still a token for the parser to report.
    /// </summary>
    public sealed class ScriptTokenizerTests
    {
        private static IReadOnlyList<ScriptToken> Tokenize(string text)
        {
            return ScriptTokenizer.Tokenize(ScriptLineSplitter.Split(text).Single());
        }

        // Each token as its kind and value, so an expectation reads like the line.
        private static string Describe(string text)
        {
            return string.Join(" | ", Tokenize(text).Select(x => $"{x.Kind} {x.Value}".TrimEnd()));
        }

        [Fact]
        public void Every_catalog_word_is_one_keyword()
        {
            List<string> wrong = new List<string>();

            foreach (string text in ScriptKeywordCatalog.All.Select(x => x.Text).Distinct())
            {
                ScriptToken first = Tokenize(text)[0];
                if (first.Kind != ScriptTokenKindEnum.KEYWORD || first.Value != text)
                    wrong.Add($"{text}: {first.Kind} {first.Value}");
            }

            wrong.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("Wait Until No Image <[ x ]>", "KEYWORD Wait Until No Image | KEYWORD <[ | QUOTE x | KEYWORD ]> | END_OF_LINE")]
        [InlineData("Go Back to <[ x ]>", "KEYWORD Go Back | KEYWORD to | KEYWORD <[ | QUOTE x | KEYWORD ]> | END_OF_LINE")]
        [InlineData("on   screen   offset 1 2", "KEYWORD on screen | KEYWORD offset | NUMBER 1 | NUMBER 2 | END_OF_LINE")]
        [InlineData("is not empty", "KEYWORD is not empty | END_OF_LINE")]
        [InlineData("is not <[ x ]>", "KEYWORD is not | KEYWORD <[ | QUOTE x | KEYWORD ]> | END_OF_LINE")]
        [InlineData("match shape and brightness", "KEYWORD match | KEYWORD shape and brightness | END_OF_LINE")]
        public void Words_join_into_the_longest_keyword(string line, string expected)
        {
            Describe(line).ShouldBe(expected);
        }

        [Theory]
        [InlineData("Wait 800ms to 1200ms", "KEYWORD Wait | NUMBER 800ms | KEYWORD to | NUMBER 1200ms | END_OF_LINE")]
        [InlineData("offset -10 0.88", "KEYWORD offset | NUMBER -10 | NUMBER 0.88 | END_OF_LINE")]
        [InlineData("captured 1920x1080 at 120dpi", "KEYWORD captured | NUMBER 1920x1080 | KEYWORD at | NUMBER 120dpi | END_OF_LINE")]
        [InlineData("Fnid Image <[ x ]>", "UNKNOWN Fnid | UNKNOWN Image | KEYWORD <[ | QUOTE x | KEYWORD ]> | END_OF_LINE")]
        [InlineData("Press Ctrl+C", "KEYWORD Press | UNKNOWN Ctrl+C | END_OF_LINE")]
        [InlineData("Loop - times", "KEYWORD Loop | UNKNOWN - | KEYWORD times | END_OF_LINE")]
        public void A_word_that_is_no_keyword_is_a_number_when_it_starts_like_one(string line, string expected)
        {
            Describe(line).ShouldBe(expected);
        }

        // Nothing inside the quotes is special, and the spaces just inside them are layout rather than text.
        [Theory]
        [InlineData("<[ He said \"hi\" ]>", "He said \"hi\"")]
        [InlineData(@"<[ C:\temp\ ]>", @"C:\temp\")]
        [InlineData("<[This text ]>", "This text")]
        [InlineData("<[                  This text ]>", "This text")]
        [InlineData(@"<[ total:  (\d+) ]>", @"total:  (\d+)")]
        [InlineData("<[ Wait to in at ]>", "Wait to in at")]
        [InlineData("<[]>", "")]
        public void A_quote_is_everything_between_its_symbols_trimmed(string line, string value)
        {
            IReadOnlyList<ScriptToken> tokens = Tokenize(line);

            tokens.Select(x => x.Kind).ShouldBe([ScriptTokenKindEnum.KEYWORD, ScriptTokenKindEnum.QUOTE, ScriptTokenKindEnum.KEYWORD, ScriptTokenKindEnum.END_OF_LINE]);
            tokens[1].Value.ShouldBe(value);
        }

        // The tokenizer only cuts. Whether a quote closed, or closed twice, is for the parser to say.
        // A quote opens only where a word would start.
        [Theory]
        [InlineData("<[ no end", "KEYWORD <[ | QUOTE no end | END_OF_LINE")]
        [InlineData("<[ a <[ b ]>", "KEYWORD <[ | QUOTE a <[ b | KEYWORD ]> | END_OF_LINE")]
        [InlineData("<[ a ]> b ]>", "KEYWORD <[ | QUOTE a | KEYWORD ]> | UNKNOWN b | KEYWORD ]> | END_OF_LINE")]
        [InlineData("<[ a ]>b", "KEYWORD <[ | QUOTE a | KEYWORD ]> | UNKNOWN b | END_OF_LINE")]
        [InlineData("Notify<[x]>", "UNKNOWN Notify<[x]> | END_OF_LINE")]
        public void A_quote_never_fails_and_opens_only_where_a_word_starts(string line, string expected)
        {
            Describe(line).ShouldBe(expected);
        }

        [Theory]
        [InlineData("# A fresh profile, to go in at once", "KEYWORD # | QUOTE A fresh profile, to go in at once | END_OF_LINE")]
        [InlineData("#no space", "KEYWORD # | QUOTE no space | END_OF_LINE")]
        [InlineData("#", "KEYWORD # | QUOTE | END_OF_LINE")]
        [InlineData("## Sign in", "KEYWORD ## | QUOTE Sign in | END_OF_LINE")]
        [InlineData("Flow:    Login and add to cart", "KEYWORD Flow: | QUOTE Login and add to cart | END_OF_LINE")]
        [InlineData("Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111", "KEYWORD Id: | QUOTE 8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111 | END_OF_LINE")]
        [InlineData("Notify # later", "KEYWORD Notify | KEYWORD # | UNKNOWN later | END_OF_LINE")]
        public void A_keyword_opening_a_line_can_take_the_rest_of_it_as_written(string line, string expected)
        {
            Describe(line).ShouldBe(expected);
        }

        [Fact]
        public void Each_token_starts_where_it_was_written_and_the_end_is_just_past_the_text()
        {
            Tokenize(" Move to <[ Find ]>").Select(x => x.Column).ShouldBe([2, 7, 10, 13, 18, 20]);
        }

        [Fact]
        public void A_quote_starts_at_its_text_not_the_spaces_before_it()
        {
            Tokenize("Id:      nope")[1].Column.ShouldBe(10);
        }

        [Fact]
        public void A_keyword_of_several_words_starts_at_its_first()
        {
            Tokenize("  on   screen")[0].Column.ShouldBe(3);
        }

        [Fact]
        public void A_blank_line_is_only_its_end()
        {
            Describe("").ShouldBe("END_OF_LINE");
            Tokenize("")[0].Column.ShouldBe(1);
        }

        [Fact]
        public void Every_token_knows_its_line()
        {
            ScriptLine line = ScriptLineSplitter.Split("Steps:\nWait 800ms")[1];

            ScriptTokenizer.Tokenize(line).ShouldAllBe(x => x.Line == 2);
        }
    }
}
