using Core.Enums;
using Core.Helpers;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepTreeNodeHandler : IRequestHandler<GetFlowStepTreeNodeQuery, ResultDto<IEnumerable<TreeNodeDto>>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTreeNodeHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IEnumerable<TreeNodeDto>>> Handle(GetFlowStepTreeNodeQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Expanding the Flow node asks for its root steps, expanding a FlowStep asks for its
            // children. Both arrive here, so the caller says which one it is: matching the id
            // against both columns would let a FlowStep adopt the root steps of the Flow that
            // happens to share its id.
            IQueryable<Core.Models.Database.FlowStep> query = request.dto.IsFlow
                ? dbContext.FlowSteps.Where(x => x.FlowId == request.dto.Id && x.ParentFlowStepId == null)
                : dbContext.FlowSteps.Where(x => x.ParentFlowStepId == request.dto.Id);

            List<TreeNodeDto> children = await query
                .AsNoTracking()
                .OrderBy(x => x.OrderNumber)
                .Select(x => new TreeNodeDto
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

                    // Projected, not Included: the row needs names and counts, never the entities
                    // behind them, and the template blob must not come along.
                    Detail = new TreeNodeDetailDto
                    {
                        WaitForMilliseconds = x.WaitForMilliseconds,
                        LoopCount = x.LoopCount,
                        IsLoopInfinite = x.IsLoopInfinite,

                        AreaName = x.FlowArea != null ? x.FlowArea.Name : null,
                        PointName = x.FlowPoint != null ? x.FlowPoint.Name : null,
                        PointEndName = x.FlowPointEnd != null ? x.FlowPointEnd.Name : null,
                        ReferenceStepName = x.FlowStepReference != null ? x.FlowStepReference.Name : null,
                        ReferenceStepEndName = x.FlowStepReferenceEnd != null ? x.FlowStepReferenceEnd.Name : null,
                        SubFlowName = x.InvokedFlow != null ? x.InvokedFlow.Name : null,

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
                })
                .ToListAsync(ct);

            // Set outside the projection so the rules live in one helper rather than as inline
            // expressions EF has to translate.
            foreach (TreeNodeDto child in children)
            {
                FlowStepTypeEnum type = child.flowStepType!.Value;

                child.Key = TreeNodeDto.BuildKey(child.EntityId, isFlow: false);
                child.Droppable = TreeStepHelper.CanContainChildren(type);
                child.Leaf = TreeStepHelper.IsLeaf(type);

                // Success and Failure are structural: the user did not add them and moving one
                // would detach a branch from the step that owns it.
                child.Draggable = !TreeStepHelper.IsBranchChild(type);
            }

            return ResultDto<IEnumerable<TreeNodeDto>>.Success(children);
        }
    }
}
