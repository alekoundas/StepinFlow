using Core.Enums;

namespace Core.Helpers
{
    public static class TreeStepHelper
    {
        /// <summary>
        /// Types that get a Success and a Failure child created with them. Steps go under those
        /// branches, never directly under the step itself.
        /// </summary>
        private static readonly FlowStepTypeEnum[] BranchTypes =
        [
            FlowStepTypeEnum.IMAGE_SEARCH,
            FlowStepTypeEnum.TEXT_SEARCH,
            FlowStepTypeEnum.SYSTEM_COMMAND,
        ];

        /// <summary>Types the user can drop steps into.</summary>
        private static readonly FlowStepTypeEnum[] ContainerTypes =
        [
            FlowStepTypeEnum.SUCCESS,
            FlowStepTypeEnum.FAILURE,
            FlowStepTypeEnum.LOOP,
        ];

        /// <summary>Structural nodes the user did not create and must not move or delete.</summary>
        private static readonly FlowStepTypeEnum[] BranchChildTypes =
        [
            FlowStepTypeEnum.SUCCESS,
            FlowStepTypeEnum.FAILURE,
        ];

        public static bool HasBranchChildren(FlowStepTypeEnum type) => BranchTypes.Contains(type);

        public static bool CanContainChildren(FlowStepTypeEnum type) => ContainerTypes.Contains(type);

        public static bool IsBranchChild(FlowStepTypeEnum type) => BranchChildTypes.Contains(type);

        /// <summary>A branch step holds nothing directly but still expands, to reveal its branches.</summary>
        public static bool IsLeaf(FlowStepTypeEnum type) =>
            !CanContainChildren(type) && !HasBranchChildren(type);
    }
}
