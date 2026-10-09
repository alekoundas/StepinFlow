using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Flows
{
    /// <summary>
    /// Tree maths for drag and drop, shared by the preview query and the move command so both
    /// answer identically. Works on the full step list of one root, which is one query thanks to
    /// FlowStep.RootId.
    /// </summary>
    public static class TreeStepMoveHelper
    {
        /// <summary>
        /// Rejects moves that would corrupt the tree. Returns null when the move is allowed.
        /// </summary>
        public static string? Validate(IReadOnlyList<FlowStep> steps, FlowStepMoveDto dto)
        {
            FlowStep? moved = steps.FirstOrDefault(x => x.Id == dto.FlowStepId);
            if (moved == null)
                return "The step being moved no longer exists.";

            if (dto.TargetParentFlowStepId == null && dto.TargetFlowId == null)
                return "A move needs a destination.";

            if (dto.TargetParentFlowStepId != null && dto.TargetFlowId != null)
                return "A step lands either under another step or at the root of the flow, not both.";

            if (dto.TargetParentFlowStepId != null)
            {
                if (dto.TargetParentFlowStepId == dto.FlowStepId)
                    return "A step cannot be dropped into itself.";

                FlowStep? targetParent = steps.FirstOrDefault(x => x.Id == dto.TargetParentFlowStepId);
                if (targetParent == null)
                    return "The destination step no longer exists.";

                // A branching step owns Success and Failure and nothing else, so a drop beside one
                // of those branches has to be refused here too: the tree is only one of the ways a
                // move can arrive.
                if (!TreeStepHelper.CanContainChildren(targetParent.FlowStepType))
                    return $"\"{targetParent.Name}\" holds steps in its branches, not directly.";

                // The cycle case: dropping a step inside its own subtree would detach that subtree
                // from the tree entirely.
                if (GetDescendantIds(steps, dto.FlowStepId).Contains(dto.TargetParentFlowStepId.Value))
                    return "A step cannot be dropped inside one of its own children.";
            }

            return null;
        }

        /// <summary>
        /// Every step below <paramref name="stepId"/>, excluding the step itself.
        /// </summary>
        public static HashSet<int> GetDescendantIds(IReadOnlyList<FlowStep> steps, int stepId)
        {
            ILookup<int?, FlowStep> childrenByParent = steps.ToLookup(x => x.ParentFlowStepId);

            HashSet<int> descendants = new HashSet<int>();
            Stack<int> pending = new Stack<int>();
            pending.Push(stepId);

            while (pending.Count > 0)
            {
                int currentId = pending.Pop();

                foreach (FlowStep child in childrenByParent[currentId])
                {
                    if (descendants.Add(child.Id))
                        pending.Push(child.Id);
                }
            }

            return descendants;
        }

        /// <summary>
        /// A step that reads another step's result needs that step to be readable from where it
        /// lands, which TreeStepHelper defines and the validator enforces. Re-parenting can quietly
        /// break that, which at runtime means acting on a stale result rather than failing, so the
        /// user is told before the move commits.
        ///
        /// The same rule on purpose: a weaker one here would let a step be dropped into a Failure
        /// branch without warning and only fail validation afterwards. A Go Back and a Notify are
        /// held to their own rules the same way: a Go Back's target has to stay behind it, and a
        /// Notify has to stay under the Failure branch of the step it reports on.
        ///
        /// Only references that are valid now and broken afterwards are reported: pre-existing
        /// breakage is not this move's fault.
        /// </summary>
        public static List<FlowStepBrokenReferenceDto> FindBrokenReferences(IReadOnlyList<FlowStep> steps, FlowStepMoveDto dto)
        {
            Dictionary<int, StepChainNode> before = steps.ToDictionary(
                x => x.Id,
                x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name, x.OrderNumber));

            Dictionary<int, StepChainNode> after = AfterMove(before, dto);

            List<FlowStepBrokenReferenceDto> broken = new List<FlowStepBrokenReferenceDto>();

            foreach (FlowStep step in steps)
            {
                AddIfBroken(step, step.FlowStepReferenceId, isEndReference: false);
                AddIfBroken(step, step.FlowStepReferenceEndId, isEndReference: true);
            }

            return broken;

            void AddIfBroken(FlowStep step, int? referenceId, bool isEndReference)
            {
                if (referenceId == null)
                    return;

                bool wasValid = CanReach(before, step, referenceId.Value);
                bool isValid = CanReach(after, step, referenceId.Value);

                if (!wasValid || isValid)
                    return;

                broken.Add(new FlowStepBrokenReferenceDto
                {
                    FlowStepId = step.Id,
                    FlowStepName = step.Name,
                    ReferencedStepName = before.TryGetValue(referenceId.Value, out StepChainNode r) ? r.Name : string.Empty,
                    IsEndReference = isEndReference,
                });
            }
        }

        /// <summary>
        /// Renumbers a sibling list 0..n-1 with the moved step inserted at the requested index.
        /// Called for the destination, and for the source when the parent changed.
        /// </summary>
        public static void ApplyOrder(List<FlowStep> siblings, FlowStep? moved, int targetIndex)
        {
            List<FlowStep> ordered = siblings
                .Where(x => moved == null || x.Id != moved.Id)
                .OrderBy(x => x.OrderNumber)
                .ToList();

            if (moved != null)
                ordered.Insert(Math.Clamp(targetIndex, 0, ordered.Count), moved);

            for (int index = 0; index < ordered.Count; index++)
                ordered[index].OrderNumber = index;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool CanReach(IReadOnlyDictionary<int, StepChainNode> byId, FlowStep step, int referenceId)
        {
            if (step.FlowStepType == FlowStepTypeEnum.GO_BACK)
                return TreeStepHelper.CanGoBackTo(byId, step.Id, referenceId);

            if (step.FlowStepType == FlowStepTypeEnum.NOTIFY)
                return TreeStepHelper.CanReportFailureOf(byId, step.Id, referenceId);

            return TreeStepHelper.CanReadResultOf(byId, step.Id, referenceId);
        }

        // The new parent, and the destination renumbered the way ApplyOrder will, because a Go Back
        // cares which siblings end up above it.
        private static Dictionary<int, StepChainNode> AfterMove(Dictionary<int, StepChainNode> before, FlowStepMoveDto dto)
        {
            StepChainNode from = before[dto.FlowStepId];
            StepChainNode moved = new StepChainNode(from.Id, dto.TargetParentFlowStepId, from.FlowStepType, from.Name, from.OrderNumber);

            List<StepChainNode> siblings = before.Values
                .Where(x => x.ParentFlowStepId == dto.TargetParentFlowStepId && x.Id != dto.FlowStepId)
                .OrderBy(x => x.OrderNumber)
                .ToList();

            siblings.Insert(Math.Clamp(dto.TargetIndex, 0, siblings.Count), moved);

            Dictionary<int, StepChainNode> after = new Dictionary<int, StepChainNode>(before);
            for (int index = 0; index < siblings.Count; index++)
            {
                StepChainNode sibling = siblings[index];
                after[sibling.Id] = new StepChainNode(sibling.Id, sibling.ParentFlowStepId, sibling.FlowStepType, sibling.Name, index);
            }

            return after;
        }
    }
}
