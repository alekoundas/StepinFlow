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
            FlowStepTypeEnum.WINDOW_CLOSE,
        ];

        // The steps that verify something.
        private static readonly FlowStepTypeEnum[] CheckTypes =
        [
            FlowStepTypeEnum.SEARCH_IMAGE,
            FlowStepTypeEnum.SEARCH_TEXT,
            FlowStepTypeEnum.CHECK_VALUE,
        ];

        // The steps that keep what they read or printed, whichever way they went.
        private static readonly FlowStepTypeEnum[] ValueTypes =
        [
            FlowStepTypeEnum.SEARCH_TEXT,
            FlowStepTypeEnum.SYSTEM_COMMAND,
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

        /// <summary>
        /// A search waiting until it finds nothing is only a condition: it has no location and no
        /// value for a step below to use.
        /// </summary>
        public static bool GivesNoResult(StepChainNode step)
        {
            bool isSearch = step.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE || step.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT;

            return isSearch && step.SearchMode == SearchModeEnum.WAIT_UNTIL_NOT_FOUND;
        }

        /// <summary>Whether {{name}} of this step has anything to stand for.</summary>
        public static bool GivesValue(StepChainNode step)
        {
            return ValueTypes.Contains(step.FlowStepType) && !GivesNoResult(step);
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


        /// <summary>
        /// The steps it sits under through a Success or a Failure, nearest first, with the branch it
        /// is in. Each one's result is kept for everything under it, whichever way it went.
        /// </summary>
        public static IEnumerable<(StepChainNode Step, int Depth, FlowStepTypeEnum Branch)> BranchedAncestors(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId)
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

                FlowStepTypeEnum branch = byId[childId].FlowStepType;
                if (IsBranchChild(branch))
                    yield return (current, depth, branch);

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
                    Name = BranchName(FlowStepTypeEnum.SUCCESS),
                    OrderNumber = 0,
                    RootId = parent.RootId,
                },
                new FlowStep
                {
                    ParentFlowStep = parent,
                    FlowStepType = FlowStepTypeEnum.FAILURE,
                    Name = BranchName(FlowStepTypeEnum.FAILURE),
                    OrderNumber = 1,
                    RootId = parent.RootId,
                }
            ];
        }

        /// <summary>A Success, a Failure or a stage marker executes nothing of its own, so there is nothing to skip.</summary>
        public static bool CanBeDisabled(FlowStepTypeEnum type)
        {
            return !IsBranchChild(type) && type != FlowStepTypeEnum.STAGE_MARKER;
        }

        /// <summary>The steps an execution can reach: a disabled step and everything under it are left out.</summary>
        public static List<FlowStep> WithoutDisabled(IReadOnlyCollection<FlowStep> steps)
        {
            Dictionary<int, FlowStep> byId = steps.ToDictionary(x => x.Id);

            return steps.Where(x => !IsSkipped(x, byId)).ToList();
        }

        private static bool IsSkipped(FlowStep step, Dictionary<int, FlowStep> byId)
        {
            FlowStep? current = step;

            // Bounded by the step count, so a corrupt parent chain cannot spin forever.
            int guard = byId.Count + 1;

            while (current != null && guard-- > 0)
            {
                if (current.IsDisabled)
                    return true;

                if (current.ParentFlowStepId == null)
                    return false;

                current = byId.GetValueOrDefault(current.ParentFlowStepId.Value);
            }

            return false;
        }

        public static string BranchName(FlowStepTypeEnum type)
        {
            if (type == FlowStepTypeEnum.FAILURE)
                return "Failure";

            return "Success";
        }

        public static bool CanReadResultOf(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId, int referenceId)
        {
            return SuccessfulAncestors(byId, fromStepId).Any(x => x.Step.Id == referenceId);
        }

        /// <summary>
        /// Whether {{name}} here can be filled in from that step. A value is kept whichever way the
        /// step went, so either side of it will do - a location only exists on the Success side.
        /// </summary>
        public static bool CanReadValueOf(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId, int referenceId)
        {
            return BranchedAncestors(byId, fromStepId).Any(x => x.Step.Id == referenceId);
        }

        /// <summary>Whether a Notify here can include that step's result: one it sits under, either side.</summary>
        public static bool CanReportOn(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId, int referenceId)
        {
            return BranchedAncestors(byId, fromStepId).Any(x => x.Step.Id == referenceId);
        }

        /// <summary>
        /// What a Break can leave, nearest first: the steps it sits inside, up to the top of its flow.
        /// Never a Success or Failure row - leaving one is leaving its check.
        /// </summary>
        public static IEnumerable<StepChainNode> BreakTargets(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId)
        {
            if (!byId.TryGetValue(fromStepId, out StepChainNode current))
                yield break;

            // Bounded by the step count, so a corrupt parent chain cannot spin forever.
            int guard = byId.Count + 1;

            while (current.ParentFlowStepId != null && guard-- > 0)
            {
                if (!byId.TryGetValue(current.ParentFlowStepId.Value, out StepChainNode parent))
                    yield break;

                if (!IsBranchChild(parent.FlowStepType))
                    yield return parent;

                current = parent;
            }
        }

        public static bool CanBreakOutOf(IReadOnlyDictionary<int, StepChainNode> byId, int fromStepId, int targetId)
        {
            return BreakTargets(byId, fromStepId).Any(x => x.Id == targetId);
        }
    }
}
