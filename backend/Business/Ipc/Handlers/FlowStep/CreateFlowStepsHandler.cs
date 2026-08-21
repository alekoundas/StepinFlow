using AutoMapper;
using Business.Helpers;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Saves a whole draft at once.
    ///
    /// One transaction on purpose: a wizard that wrote twelve of twenty steps and then failed
    /// would leave the flow in a state the user never asked for and cannot easily undo.
    ///
    /// Source agnostic by design. The recorder builds the draft today and the AI will build it
    /// later; neither of them appears anywhere below this line.
    /// </summary>
    public class CreateFlowStepsHandler : IRequestHandler<CreateFlowStepsCommand, ResultDto<FlowDraftResultDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepsHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowDraftResultDto>> Handle(CreateFlowStepsCommand request, CancellationToken ct)
        {
            FlowDraftDto draft = request.dto;

            if (draft.Steps.Count == 0)
                return ResultDto<FlowDraftResultDto>.Failure("There is nothing to save.");

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                Flow? flow = await ResolveFlowAsync(dbContext, draft.Target, ct);
                if (flow == null)
                    return ResultDto<FlowDraftResultDto>.Failure("The flow to save into no longer exists.");

                int? parentStepId = draft.Target.TargetParentFlowStepId;
                int rootId = flow.Id;

                if (parentStepId != null)
                {
                    FlowStep? parent = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == parentStepId, ct);
                    if (parent == null)
                        return ResultDto<FlowDraftResultDto>.Failure("The step to save under no longer exists.");

                    if (!TreeStepHelper.CanContainChildren(parent.FlowStepType))
                        return ResultDto<FlowDraftResultDto>.Failure($"\"{parent.Name}\" holds steps in its branches, not directly.");

                    rootId = parent.RootId;
                }

                Dictionary<int, FlowStep> stepByTempId = new Dictionary<int, FlowStep>();
                List<FlowStep> rootLevel = new List<FlowStep>();

                foreach (DraftStepDto draftStep in draft.Steps)
                {
                    FlowStep step = _mapper.Map<FlowStep>(draftStep.Values);
                    step.Id = 0;
                    step.RootId = rootId;
                    step.FlowId = null;
                    step.ParentFlowStepId = null;
                    step.ParentFlowStep = null;

                    AttachPoints(dbContext, flow, step, draftStep);

                    // A step with no parent inside the draft lands at the target position; one
                    // with a parent hangs off it wherever that ends up.
                    if (draftStep.ParentTempId is int parentTempId && stepByTempId.TryGetValue(parentTempId, out FlowStep? draftParent))
                    {
                        step.ParentFlowStep = draftParent;
                    }
                    else if (parentStepId != null)
                    {
                        step.ParentFlowStepId = parentStepId;
                        rootLevel.Add(step);
                    }
                    else
                    {
                        step.FlowId = flow.Id;
                        rootLevel.Add(step);
                    }

                    dbContext.FlowSteps.Add(step);
                    FlowStepImageSyncHelper.Sync(dbContext, step, draftStep.Values.FlowStepImages);
                    dbContext.FlowSteps.AddRange(TreeStepHelper.CreateBranchChildren(step));

                    stepByTempId[draftStep.TempId] = step;
                }

                // Saved before renumbering so the new steps have real ids to be ordered against.
                await dbContext.SaveChangesAsync(ct);

                await RenumberDestinationAsync(dbContext, draft.Target, parentStepId, flow.Id, rootLevel, ct);
                await dbContext.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return ResultDto<FlowDraftResultDto>.Success(new FlowDraftResultDto
                {
                    FlowId = flow.Id,
                    FirstFlowStepId = rootLevel.FirstOrDefault()?.Id ?? stepByTempId.Values.First().Id,
                    CreatedCount = stepByTempId.Count,
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return ResultDto<FlowDraftResultDto>.Failure(ex.Message);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<Flow?> ResolveFlowAsync(AppDbContext dbContext, FlowDraftTargetDto target, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(target.NewFlowName))
            {
                Flow created = new Flow { Name = target.NewFlowName.Trim() };
                dbContext.Flows.Add(created);
                await dbContext.SaveChangesAsync(ct);
                return created;
            }

            if (target.TargetFlowId is int flowId)
                return await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == flowId, ct);

            if (target.TargetParentFlowStepId is int parentId)
            {
                int rootId = await dbContext.FlowSteps
                    .Where(x => x.Id == parentId)
                    .Select(x => x.RootId)
                    .FirstOrDefaultAsync(ct);

                return await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == rootId, ct);
            }

            return null;
        }

        /// <summary>
        /// A recorded click knows where it happened, but a cursor step reads its position from a
        /// FlowPoint. Creating them here is what makes a recording runnable without the user
        /// having to place every point by hand.
        /// </summary>
        private static void AttachPoints(AppDbContext dbContext, Flow flow, FlowStep step, DraftStepDto draftStep)
        {
            if (draftStep.NewPoint is DraftPointDto start)
            {
                FlowPoint point = NewPoint(flow, start);
                dbContext.FlowPoints.Add(point);
                step.FlowPoint = point;
                step.FlowPointId = null;
            }

            if (draftStep.NewPointEnd is DraftPointDto end)
            {
                FlowPoint point = NewPoint(flow, end);
                dbContext.FlowPoints.Add(point);
                step.FlowPointEnd = point;
                step.FlowPointEndId = null;
            }
        }

        private static FlowPoint NewPoint(Flow flow, DraftPointDto dto) => new FlowPoint
        {
            Flow = flow,
            Name = dto.Name,
            LocationX = dto.LocationX,
            LocationY = dto.LocationY,
        };

        /// <summary>
        /// Slots the new steps into the destination at the requested index and renumbers the
        /// siblings around them, reusing the ordering a drag and drop already uses.
        /// </summary>
        private static async Task RenumberDestinationAsync(
            AppDbContext dbContext,
            FlowDraftTargetDto target,
            int? parentStepId,
            int flowId,
            List<FlowStep> inserted,
            CancellationToken ct)
        {
            if (inserted.Count == 0)
                return;

            List<FlowStep> siblings = parentStepId != null
                ? await dbContext.FlowSteps.Where(x => x.ParentFlowStepId == parentStepId).ToListAsync(ct)
                : await dbContext.FlowSteps.Where(x => x.ParentFlowStepId == null && x.FlowId == flowId).ToListAsync(ct);

            List<FlowStep> existing = siblings
                .Where(x => inserted.All(y => y.Id != x.Id))
                .OrderBy(x => x.OrderNumber)
                .ToList();

            int index = Math.Clamp(target.TargetIndex, 0, existing.Count);
            existing.InsertRange(index, inserted);

            for (int i = 0; i < existing.Count; i++)
                existing[i].OrderNumber = i;
        }
    }
}
