using Core.Enums;
using Core.Models.Business;

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
            FlowStepTypeEnum.READ_TEXT,
            FlowStepTypeEnum.SYSTEM_COMMAND,
            FlowStepTypeEnum.CHECK_VALUE,
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

        /// <summary>
        /// The ancestors of <paramref name="fromStepId"/> whose result can be read there, nearest
        /// first. A result only exists once the step that produced it has run and succeeded, so the
        /// way down from it has to be its Success branch. Anywhere else is either the failure path
        /// or a step that may not have run.
        ///
        /// The starting node is never returned, which is what lets the "add a step" case pass the
        /// branch the new step will live under and get the same answer as the saved step would.
        /// </summary>
        public static IEnumerable<(StepChainNode Step, int Depth)> ReadableAncestors(
            IReadOnlyDictionary<int, StepChainNode> byId,
            int fromStepId)
        {
            if (!byId.TryGetValue(fromStepId, out StepChainNode from))
                yield break;

            int childId = fromStepId;
            int? currentId = from.ParentFlowStepId;
            int depth = 1;

            // Bounded by the step count, so a corrupt parent chain cannot spin forever.
            int guard = byId.Count + 1;

            while (currentId != null && guard-- > 0)
            {
                if (!byId.TryGetValue(currentId.Value, out StepChainNode current))
                    yield break;

                if (byId[childId].FlowStepType == FlowStepTypeEnum.SUCCESS)
                    yield return (current, depth);

                childId = current.Id;
                currentId = current.ParentFlowStepId;
                depth++;
            }
        }

        public static bool CanReadResultOf(
            IReadOnlyDictionary<int, StepChainNode> byId,
            int fromStepId,
            int referenceId) =>
            ReadableAncestors(byId, fromStepId).Any(x => x.Step.Id == referenceId);
    }
}
