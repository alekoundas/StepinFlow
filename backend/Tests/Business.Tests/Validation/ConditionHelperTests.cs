using Business.Validation;
using Core.Enums;

namespace Business.Tests.Validation
{
    public sealed class ConditionHelperTests
    {
        [Theory]
        [InlineData(ConditionTypeEnum.IS_EMPTY, false, false)]
        [InlineData(ConditionTypeEnum.IS_NOT_EMPTY, false, false)]
        [InlineData(ConditionTypeEnum.EQUALS, true, false)]
        [InlineData(ConditionTypeEnum.MATCHES_REGEX, true, false)]
        [InlineData(ConditionTypeEnum.BETWEEN, true, true)]
        public void Each_condition_asks_for_as_many_values_as_it_compares(ConditionTypeEnum condition, bool needsValue, bool needsSecond)
        {
            ConditionHelper.NeedsValue(condition).ShouldBe(needsValue);
            ConditionHelper.NeedsSecondValue(condition).ShouldBe(needsSecond);
        }
    }
}
