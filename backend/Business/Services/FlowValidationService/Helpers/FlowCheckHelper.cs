using Core.Enums;
using Core.Helpers;
using Core.Models.Business;

namespace Business.Services.FlowValidationService.Helpers
{
    /// <summary>
    /// What a flow verifies, calculated by walking the tree.
    /// </summary>
    public static class FlowCheckHelper
    {
        public static IReadOnlyList<FlowCheck> Build(IReadOnlyList<FlowCheckNode> steps)
        {
            List<FlowCheck> flowChecks = new List<FlowCheck>();
            Dictionary<int, FlowCheckNode> byId = steps.ToDictionary(x => x.Id);

            Dictionary<int, List<FlowCheckNode>> childrenByParent = steps
                .Where(x => x.ParentFlowStepId != null)
                .GroupBy(x => x.ParentFlowStepId!.Value)
                .ToDictionary(x => x.Key, x => x.OrderBy(c => c.OrderNumber).ToList());

            List<FlowCheckNode> markers = steps
                .Where(x => x.ParentFlowStepId == null && x.FlowStepType == FlowStepTypeEnum.MARKER)
                .OrderBy(x => x.OrderNumber)
                .ToList();


            foreach (FlowCheckNode step in steps.Where(x => TreeStepHelper.IsCheck(x.FlowStepType)))
            {
                FlowCheckNode? ending = FailureEnding(step, byId, childrenByParent);

                flowChecks.Add(new FlowCheck
                {
                    FlowStepId = step.Id,
                    FlowStepType = step.FlowStepType,
                    Name = step.Name,
                    CodeComment = step.CodeComment,
                    MarkerName = MarkerOf(step, byId, markers),
                    IsFatal = ending != null,
                    FailureMessage = ending?.Message,
                });
            }

            return flowChecks;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // The nearest End Execution that failing this check leads to, or null when nothing stops.
        // Breadth first, so the most direct consequence wins over one buried deeper.
        //
        // The whole Failure subtree counts, not just its immediate children: an End Execution under
        // a nested check is still where this check's failure ends up, whichever way that nested
        // check went.
        private static FlowCheckNode? FailureEnding(FlowCheckNode check, IReadOnlyDictionary<int, FlowCheckNode> byId, IReadOnlyDictionary<int, List<FlowCheckNode>> childrenByParent)
        {
            if (!childrenByParent.TryGetValue(check.Id, out List<FlowCheckNode>? branches))
                return null;

            FlowCheckNode? failure = branches.FirstOrDefault(x => x.FlowStepType == FlowStepTypeEnum.FAILURE);
            if (failure == null)
                return null;

            Queue<FlowCheckNode> pending = new Queue<FlowCheckNode>();
            pending.Enqueue(failure);

            // Bounded by the step count, so a corrupt parent chain cannot spin forever.
            int guard = byId.Count + 1;

            while (pending.Count > 0 && guard-- > 0)
            {
                FlowCheckNode current = pending.Dequeue();

                if (current.FlowStepType == FlowStepTypeEnum.END_EXECUTION && !current.EndExecutionAsSuccess)
                    return current;

                if (!childrenByParent.TryGetValue(current.Id, out List<FlowCheckNode>? children))
                    continue;

                foreach (FlowCheckNode child in children)
                    pending.Enqueue(child);
            }

            return null;
        }

        // Sections do not nest and do not indent, so the marker that owns a step is the last one
        // above the top level step this one sits under - not above the step itself, which may be
        // several branches deep.
        private static string? MarkerOf(FlowCheckNode step, IReadOnlyDictionary<int, FlowCheckNode> byId, IReadOnlyList<FlowCheckNode> markers)
        {
            if (markers.Count == 0)
                return null;

            FlowCheckNode current = step;
            int guard = byId.Count + 1;

            while (current.ParentFlowStepId != null && guard-- > 0)
            {
                if (!byId.TryGetValue(current.ParentFlowStepId.Value, out FlowCheckNode? parent))
                    return null;

                current = parent;
            }

            return markers.LastOrDefault(x => x.OrderNumber < current.OrderNumber)?.Name;
        }
    }
}
