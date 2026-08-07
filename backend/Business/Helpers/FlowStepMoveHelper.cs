using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Helpers
{
    /// <summary>
    /// Tree maths for drag and drop, shared by the preview query and the move command so both
    /// answer identically. Works on the full step list of one root, which is one query thanks to
    /// FlowStep.RootId.
    /// </summary>
    public static class FlowStepMoveHelper
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
        /// A cursor step reads the result of an IMAGE_SEARCH / TEXT_SEARCH that must be one of its
        /// ancestors, otherwise the referenced step may not have run yet. Re-parenting can quietly
        /// break that, which at runtime means clicking the wrong place rather than failing, so the
        /// user is told before the move commits.
        ///
        /// Only references that are valid now and broken afterwards are reported: pre-existing
        /// breakage is not this move's fault.
        /// </summary>
        public static List<FlowStepBrokenReferenceDto> FindBrokenReferences(
            IReadOnlyList<FlowStep> steps,
            FlowStepMoveDto dto)
        {
            Dictionary<int, int?> parentBefore = steps.ToDictionary(x => x.Id, x => x.ParentFlowStepId);

            Dictionary<int, int?> parentAfter = new Dictionary<int, int?>(parentBefore)
            {
                [dto.FlowStepId] = dto.TargetParentFlowStepId,
            };

            Dictionary<int, string> nameById = steps.ToDictionary(x => x.Id, x => x.Name);

            List<FlowStepBrokenReferenceDto> broken = new List<FlowStepBrokenReferenceDto>();

            foreach (FlowStep step in steps)
            {
                AddIfBroken(step.Id, step.FlowStepReferenceId, isEndReference: false);
                AddIfBroken(step.Id, step.FlowStepReferenceEndId, isEndReference: true);
            }

            return broken;

            void AddIfBroken(int stepId, int? referenceId, bool isEndReference)
            {
                if (referenceId == null)
                    return;

                bool wasValid = IsAncestorOf(parentBefore, referenceId.Value, stepId);
                bool isValid = IsAncestorOf(parentAfter, referenceId.Value, stepId);

                if (!wasValid || isValid)
                    return;

                broken.Add(new FlowStepBrokenReferenceDto
                {
                    FlowStepId = stepId,
                    FlowStepName = nameById.TryGetValue(stepId, out string? stepName) ? stepName : string.Empty,
                    ReferencedStepName = nameById.TryGetValue(referenceId.Value, out string? refName) ? refName : string.Empty,
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

        private static bool IsAncestorOf(Dictionary<int, int?> parentById, int ancestorId, int stepId)
        {
            int? currentId = parentById.TryGetValue(stepId, out int? parentId) ? parentId : null;

            // Bounded by the number of steps, so a corrupt parent chain cannot spin forever.
            int guard = parentById.Count + 1;

            while (currentId != null && guard-- > 0)
            {
                if (currentId.Value == ancestorId)
                    return true;

                currentId = parentById.TryGetValue(currentId.Value, out int? nextId) ? nextId : null;
            }

            return false;
        }
    }
}
