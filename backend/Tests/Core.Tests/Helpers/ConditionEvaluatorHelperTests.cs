using Core.Enums;
using Core.Helpers;

namespace Core.Tests.Helpers
{
    public sealed class ConditionEvaluatorHelperTests
    {
        [Theory]
        [InlineData("Products", ConditionTypeEnum.EQUALS, "products", true)]
        [InlineData("Products", ConditionTypeEnum.EQUALS, "Product", false)]
        [InlineData("Products", ConditionTypeEnum.NOT_EQUALS, "Cart", true)]
        [InlineData("Total: 42", ConditionTypeEnum.CONTAINS, "TOTAL", true)]
        [InlineData("Total: 42", ConditionTypeEnum.NOT_CONTAINS, "error", true)]
        [InlineData("Total: 42", ConditionTypeEnum.MATCHES_REGEX, @"total: \d+", true)]
        [InlineData("   ", ConditionTypeEnum.IS_EMPTY, "", true)]
        [InlineData("x", ConditionTypeEnum.IS_NOT_EMPTY, "", true)]
        [InlineData("101", ConditionTypeEnum.GREATER_THAN, "100", true)]
        [InlineData("99.5", ConditionTypeEnum.LESS_THAN, "100", true)]
        public void Each_condition_decides_as_its_name_says(string value, ConditionTypeEnum condition, string expected, bool satisfied)
        {
            ConditionEvaluatorHelper.IsSatisfied(value, condition, expected, string.Empty).ShouldBe(satisfied);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("9", true)]
        [InlineData("5", true)]
        [InlineData("0.99", false)]
        [InlineData("9.01", false)]
        public void Between_includes_both_ends(string value, bool satisfied)
        {
            ConditionEvaluatorHelper.IsSatisfied(value, ConditionTypeEnum.BETWEEN, "1", "9").ShouldBe(satisfied);
        }

        [Theory]
        [InlineData(ConditionTypeEnum.GREATER_THAN)]
        [InlineData(ConditionTypeEnum.LESS_THAN)]
        [InlineData(ConditionTypeEnum.BETWEEN)]
        public void A_number_comparison_against_text_fails_rather_than_taking_either_side(ConditionTypeEnum condition)
        {
            ConditionEvaluatorHelper.IsSatisfied("not a number", condition, "100", "200").ShouldBeFalse();
        }

        [Fact]
        public void Numbers_are_read_the_same_whatever_the_culture()
        {
            ConditionEvaluatorHelper.IsSatisfied("1.5", ConditionTypeEnum.GREATER_THAN, "1.25", string.Empty).ShouldBeTrue();
        }

        [Fact]
        public void A_broken_regex_is_not_a_match_and_does_not_throw()
        {
            ConditionEvaluatorHelper.IsSatisfied("anything", ConditionTypeEnum.MATCHES_REGEX, "(unclosed", string.Empty).ShouldBeFalse();
        }

        [Fact]
        public void Nothing_read_counts_as_empty()
        {
            ConditionEvaluatorHelper.IsSatisfied(null, ConditionTypeEnum.IS_EMPTY, string.Empty, string.Empty).ShouldBeTrue();
        }

        [Fact]
        public void No_condition_is_never_satisfied()
        {
            ConditionEvaluatorHelper.IsSatisfied("value", null, "value", string.Empty).ShouldBeFalse();
        }
    }
}
