using Core.Enums;

namespace Business.Services.FlowValidationService
{
    public static class ConditionHelper
    {
        private static readonly ConditionTypeEnum[] WithoutValue =
        [
            ConditionTypeEnum.IS_EMPTY,
            ConditionTypeEnum.IS_NOT_EMPTY,
        ];

        public static bool NeedsValue(ConditionTypeEnum condition)
        {
            return !WithoutValue.Contains(condition);
        }

        public static bool NeedsSecondValue(ConditionTypeEnum condition)
        {
            return condition == ConditionTypeEnum.BETWEEN;
        }
    }
}
