using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;

namespace Core.Helpers
{
    public static class TreeStepHelper
    {
        // Types that get a Success and a Failure child created with them.
        private static readonly FlowStepTypeEnum[] BranchTypes =
        [
            FlowStepTypeEnum.SEARCH_IMAGE,
            FlowStepTypeEnum.SEARCH_TEXT,
            FlowStepTypeEnum.CHECK_VALUE,
            FlowStepTypeEnum.SYSTEM_COMMAND,
            FlowStepTypeEnum.WINDOW_FOCUS,
            FlowStepTypeEnum.WINDOW_RESIZE,
            FlowStepTypeEnum.WINDOW_RELOCATE,
        ];

        // The steps that verify something.
        private static readonly FlowStepTypeEnum[] CheckTypes =
        [
            FlowStepTypeEnum.SEARCH_IMAGE,
            FlowStepTypeEnum.SEARCH_TEXT,
            FlowStepTypeEnum.CHECK_VALUE,
        ];

        // Types the user can drop steps into.
        private static readonly FlowStepTypeEnum[] ContainerTypes =
        [
            FlowStepTypeEnum.SUCCESS,
            FlowStepTypeEnum.FAILURE,
            FlowStepTypeEnum.LOOP,
            FlowStepTypeEnum.END_EXECUTION,
        ];

        // Structural nodes.
        private static readonly FlowStepTypeEnum[] BranchChildTypes =
        [
            FlowStepTypeEnum.SUCCESS,
            FlowStepTypeEnum.FAILURE,
        ];

        public static bool HasBranchChildren(FlowStepTypeEnum type)
        {
            return BranchTypes.Contains(type);
        }

        public static bool CanContainChildren(FlowStepTypeEnum type)
        {
            return ContainerTypes.Contains(type);
        }

        public static bool IsCheck(FlowStepTypeEnum type)
        {
            return CheckTypes.Contains(type);
        }

        public static bool IsBranchChild(FlowStepTypeEnum type)
        {
            return BranchChildTypes.Contains(type);
        }

        public static bool IsLeaf(FlowStepTypeEnum type)
        {
            return !CanContainChildren(type) && !HasBranchChildren(type);
        }

        public static IEnumerable<(StepChainNode Step, int Depth)> SuccessfulAncestors(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId)
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


        public static IEnumerable<(StepChainNode Step, int Depth)> FailedAncestors(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId)
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

                if (byId[childId].FlowStepType == FlowStepTypeEnum.FAILURE)
                    yield return (current, depth);

                childId = current.Id;
                currentId = current.ParentFlowStepId;
                depth++;
            }
        }

        public static IReadOnlyList<FlowStep> CreateBranchChildren(FlowStep parent)
        {
            if (!HasBranchChildren(parent.FlowStepType))
                return [];

            return
            [
                new FlowStep
                {
                    ParentFlowStep = parent,
                    FlowStepType = FlowStepTypeEnum.SUCCESS,
                    Name = "Success",
                    OrderNumber = 0,
                    RootId = parent.RootId,
                },
                new FlowStep
                {
                    ParentFlowStep = parent,
                    FlowStepType = FlowStepTypeEnum.FAILURE,
                    Name = "Failure",
                    OrderNumber = 1,
                    RootId = parent.RootId,
                }
            ];
        }

        public static bool CanReadResultOf(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId, int referenceId)
        {
            return SuccessfulAncestors(byId, fromStepId).Any(x => x.Step.Id == referenceId);
        }
    }
}
