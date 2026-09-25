using Core.Helpers;
using Core.Models.Business;

namespace Core.Tests.Helpers
{
    public sealed class VariableTranslatorTests
    {
        private static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["username"] = "alex",
            ["Read the total"] = "42",
            ["width"] = "1920",
        };

        [Fact]
        public void Every_name_is_replaced_by_its_value()
        {
            VariableTranslationResult result = VariableTranslator.Translate("{{username}} has {{Read the total}} at {{width}}px", Values);

            result.Text.ShouldBe("alex has 42 at 1920px");
            result.Untranslated.ShouldBeEmpty();
        }

        [Fact]
        public void Spaces_inside_the_braces_are_not_part_of_the_name()
        {
            VariableTranslator.Translate("{{  username  }}", Values).Text.ShouldBe("alex");
        }

        [Fact]
        public void A_name_with_no_value_stays_as_written_and_is_reported()
        {
            VariableTranslationResult result = VariableTranslator.Translate("user {{password}} and {{password}}", Values);

            result.Text.ShouldBe("user {{password}} and {{password}}");
            result.Untranslated.ShouldBe(["password"]);
        }

        [Fact]
        public void Four_braces_are_an_escaped_pair()
        {
            VariableTranslator.Translate("literal {{{{username}} braces", Values).Text.ShouldBe("literal {{username}} braces");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Nothing_translates_to_nothing(string? text)
        {
            VariableTranslationResult result = VariableTranslator.Translate(text, Values);

            result.Text.ShouldBe(string.Empty);
            result.Untranslated.ShouldBeEmpty();
        }

        [Fact]
        public void Names_come_back_in_order_without_duplicates_and_without_the_escaped_ones()
        {
            VariableTranslator.Names("{{b}} {{a}} {{B}} {{{{c}} {{a}}").ShouldBe(["b", "a"]);
        }

        [Fact]
        public void The_untranslated_message_names_the_variables_as_the_author_wrote_them()
        {
            VariableTranslator.DescribeUntranslated(["password", "code"]).ShouldBe("Nothing has a value for {{password}}, {{code}}.");
        }
    }
}
