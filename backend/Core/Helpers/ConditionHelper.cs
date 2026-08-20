using Core.Enums;

namespace Core.Helpers
{
    public static class ConditionHelper
    {
        private static readonly ConditionTypeEnum[] WithoutValue =
        [
            ConditionTypeEnum.IS_EMPTY,
            ConditionTypeEnum.IS_NOT_EMPTY,
        ];

        public static bool NeedsValue(ConditionTypeEnum condition) => !WithoutValue.Contains(condition);

        public static bool NeedsSecondValue(ConditionTypeEnum condition) => condition == ConditionTypeEnum.BETWEEN;
    }
}
