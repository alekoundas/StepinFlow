using System.Linq.Expressions;

using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Core.Helpers
{
    /// <summary>
    /// What a FlowStep looks like as a tree row.
    ///
    /// Shared by the lazy tree, which asks for one level at a time, and the execution page, which
    /// takes a whole flow in one query. Two copies of this would drift, and the same step would
    /// start looking like two different things depending on which screen you were on.
    /// </summary>
    public static class FlowStepTreeNodeProjection
    {
        /// <summary>
        /// Projected, not Included: the row needs names and counts, never the entities behind them,
        /// and the template blob must not come along.
        /// </summary>
        public static Expression<Func<FlowStep, TreeNodeDto>> Row { get; } = x => new TreeNodeDto
        {
            EntityId = x.Id,
            Selectable = true,

            Name = x.Name,
            flowStepType = x.FlowStepType,
            OrderNumber = x.OrderNumber,
            IsFlow = false,
            IsNew = false,

            ParentFlowId = x.FlowId,
            ParentFlowStepId = x.ParentFlowStepId,

            Detail = new TreeNodeDetailDto
            {
                WaitForMilliseconds = x.WaitForMilliseconds,
                WaitForMillisecondsMax = x.WaitForMillisecondsMax,
                LoopCount = x.LoopCount,
                IsLoopInfinite = x.IsLoopInfinite,

                AreaName = x.FlowArea != null ? x.FlowArea.Name : null,
                PointName = x.FlowPoint != null ? x.FlowPoint.Name : null,
                PointEndName = x.FlowPointEnd != null ? x.FlowPointEnd.Name : null,
                ReferenceStepName = x.FlowStepReference != null ? x.FlowStepReference.Name : null,
                ReferenceStepEndName = x.FlowStepReferenceEnd != null ? x.FlowStepReferenceEnd.Name : null,
                SubFlowName = x.SubFlow != null ? x.SubFlow.Name : null,

                IsPointCustom = x.IsPointCustom,
                IsPointEndCustom = x.IsPointEndCustom,

                CursorButtonType = x.CursorButtonType,
                CursorButtonActionType = x.CursorButtonActionType,
                CursorScrollDirectionType = x.CursorScrollDirectionType,

                KeyboardInputText = x.KeyboardInputText,
                KeyboardInputType = x.KeyboardInputType,

                WindowWidth = x.WindowWidth,
                WindowHeight = x.WindowHeight,

                SearchMode = x.SearchMode,
                TemplateCount = x.FlowStepImages.Count(),
                Thumbnail = x.FlowStepImages
                    .OrderBy(image => image.OrderNumber)
                    .Select(image => image.Thumbnail)
                    .FirstOrDefault(),

                ConditionText = x.ConditionText,
                ConditionTextEnd = x.ConditionTextEnd,
                ConditionType = x.ConditionType,

                RunCommandShell = x.RunCommandShell,
                RunCommandPreset = x.RunCommandPreset,
                RunCommand = x.RunCommand,

                SystemActionType = x.SystemActionType,

                ChildCount = x.ChildrenFlowSteps.Count(),
            },
        };


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// The rules that are not worth making EF translate, applied to every row after it lands.
        /// </summary>
        public static void Describe(TreeNodeDto node)
        {
            FlowStepTypeEnum type = node.flowStepType!.Value;

            node.Key = TreeNodeDto.BuildKey(node.EntityId, isFlow: false);
            node.Droppable = TreeStepHelper.CanContainChildren(type);
            node.Leaf = TreeStepHelper.IsLeaf(type);

            // Success and Failure are structural: the user did not add them and moving one would
            // detach a branch from the step that owns it.
            node.Draggable = !TreeStepHelper.IsBranchChild(type);
        }
    }
}
