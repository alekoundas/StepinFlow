using Business.Helpers;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Lifts a step and everything under it into a new sub-flow, leaving a SUB_FLOW step in its
    /// place.
    ///
    /// The rows are moved, not copied. Ids survive, so a reference between two extracted steps
    /// keeps resolving with no remapping and the template images follow their step untouched.
    /// Only RootId, and the parentage of the step at the top, change. That is also why a
    /// reference crossing the boundary is the one case that has to be refused rather than fixed
    /// up: everything else takes care of itself.
    ///
    /// Search areas and points are the exception. Those belong to a flow, so the ones the moved
    /// steps use are copied into the new sub-flow and the steps repointed, which is what makes it
    /// self contained enough to call from anywhere.
    /// </summary>
    public class ExtractSubFlowHandler : IRequestHandler<ExtractSubFlowCommand, ResultDto<ExtractSubFlowResultDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ExtractSubFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<ExtractSubFlowResultDto>> Handle(ExtractSubFlowCommand request, CancellationToken ct)
        {
            ExtractSubFlowDto dto = request.dto;

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ResultDto<ExtractSubFlowResultDto>.Failure("Give the sub-flow a name.");

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                FlowStep? head = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == dto.FlowStepId, ct);
                if (head == null)
                    return ResultDto<ExtractSubFlowResultDto>.Failure("That step no longer exists.");

                if (TreeStepHelper.IsBranchChild(head.FlowStepType))
                    return ResultDto<ExtractSubFlowResultDto>.Failure(
                        "Success and Failure belong to the step above them and cannot be extracted on their own.");

                List<FlowStep> steps = await dbContext.FlowSteps
                    .Where(x => x.RootId == head.RootId)
                    .ToListAsync(ct);

                HashSet<int> moving = TreeStepMoveHelper.GetDescendantIds(steps, head.Id);
                moving.Add(head.Id);

                string? crossing = FindCrossingReference(steps, moving);
                if (crossing != null)
                    return ResultDto<ExtractSubFlowResultDto>.Failure(crossing);

                string sourceName = await dbContext.Flows
                    .Where(x => x.Id == dto.SourceRootId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(ct) ?? "another flow";

                Flow subFlow = new Flow
                {
                    Name = dto.Name.Trim(),
                    IsSubFlow = true,
                    Description = $"Extracted from {sourceName}.",
                };

                dbContext.Flows.Add(subFlow);
                await dbContext.SaveChangesAsync(ct);

                List<FlowStep> moved = steps.Where(x => moving.Contains(x.Id)).ToList();

                await CopyAreasAndPointsAsync(dbContext, subFlow, moved, ct);

                foreach (FlowStep step in moved)
                    step.RootId = subFlow.Id;

                // Only the head changes parentage: everything below it keeps pointing at its own
                // parent, which came along.
                head.ParentFlowStepId = null;
                head.ParentFlowStep = null;
                head.FlowId = subFlow.Id;
                head.OrderNumber = 0;

                FlowStep placeholder = new FlowStep
                {
                    Name = dto.Name.Trim(),
                    FlowStepType = FlowStepTypeEnum.SUB_FLOW,
                    RootId = dto.SourceRootId,
                    SubFlowId = subFlow.Id,
                    FlowId = dto.SourceFlowId,
                    ParentFlowStepId = dto.SourceParentFlowStepId,
                    OrderNumber = dto.SourceOrderNumber,
                };

                dbContext.FlowSteps.Add(placeholder);
                await dbContext.SaveChangesAsync(ct);

                RenumberSource(steps, moving, placeholder, dto);
                await dbContext.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return ResultDto<ExtractSubFlowResultDto>.Success(new ExtractSubFlowResultDto
                {
                    SubFlowId = subFlow.Id,
                    FlowStepId = placeholder.Id,
                    MovedCount = moved.Count,
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return ResultDto<ExtractSubFlowResultDto>.Failure(ex.Message);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// A reference with one end inside the selection and the other outside it would stop
        /// resolving, in a flow the user may not be looking at. Refused by name rather than
        /// cleared, so it is fixed before the extraction rather than discovered after.
        /// </summary>
        private static string? FindCrossingReference(IReadOnlyList<FlowStep> steps, HashSet<int> moving)
        {
            Dictionary<int, string> nameById = steps.ToDictionary(x => x.Id, x => x.Name);

            foreach (FlowStep step in steps)
            {
                foreach (int? referenceId in new[] { step.FlowStepReferenceId, step.FlowStepReferenceEndId })
                {
                    if (referenceId is not int target || !nameById.ContainsKey(target))
                        continue;

                    if (moving.Contains(step.Id) == moving.Contains(target))
                        continue;

                    return moving.Contains(step.Id)
                        ? $"\"{step.Name}\" reads the result of \"{nameById[target]}\", which is staying behind. Move it in, or point that step somewhere else, then extract again."
                        : $"\"{step.Name}\" reads the result of \"{nameById[target]}\", which is being extracted. Move it in, or point that step somewhere else, then extract again.";
                }
            }

            return null;
        }

        /// <summary>
        /// Areas and points belong to a flow, so the moved steps would keep pointing at ones the
        /// sub-flow cannot list or edit. Copied rather than shared: a sub-flow meant to be reused
        /// should not change when the flow it came from is edited.
        /// </summary>
        private static async Task CopyAreasAndPointsAsync(
            AppDbContext dbContext,
            Flow subFlow,
            List<FlowStep> moved,
            CancellationToken ct)
        {
            HashSet<int> areaIds = moved.Where(x => x.FlowAreaId != null).Select(x => x.FlowAreaId!.Value).ToHashSet();
            HashSet<int> pointIds = moved
                .SelectMany(x => new[] { x.FlowPointId, x.FlowPointEndId })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToHashSet();

            List<FlowArea> areas = await dbContext.FlowAreas.Where(x => areaIds.Contains(x.Id)).ToListAsync(ct);

            // A nested area needs its parent too, or the copy resolves against nothing.
            HashSet<int> parentIds = areas.Where(x => x.ParentFlowAreaId != null)
                .Select(x => x.ParentFlowAreaId!.Value)
                .Where(x => !areaIds.Contains(x))
                .ToHashSet();

            if (parentIds.Count > 0)
                areas.AddRange(await dbContext.FlowAreas.Where(x => parentIds.Contains(x.Id)).ToListAsync(ct));

            List<FlowPoint> points = await dbContext.FlowPoints.Where(x => pointIds.Contains(x.Id)).ToListAsync(ct);

            Dictionary<int, FlowArea> areaCopies = new();
            foreach (FlowArea area in areas)
            {
                FlowArea copy = new FlowArea
                {
                    Name = area.Name,
                    Type = area.Type,
                    SizingMode = area.SizingMode,
                    LocationX = area.LocationX,
                    LocationY = area.LocationY,
                    Width = area.Width,
                    Height = area.Height,
                    RatioX = area.RatioX,
                    RatioY = area.RatioY,
                    Flow = subFlow,
                };

                dbContext.FlowAreas.Add(copy);
                areaCopies[area.Id] = copy;
            }

            // Second pass: a parent copy only exists once every area has one.
            foreach (FlowArea area in areas.Where(x => x.ParentFlowAreaId != null))
                if (areaCopies.TryGetValue(area.ParentFlowAreaId!.Value, out FlowArea? parentCopy))
                    areaCopies[area.Id].ParentFlowArea = parentCopy;

            Dictionary<int, FlowPoint> pointCopies = new();
            foreach (FlowPoint point in points)
            {
                FlowPoint copy = new FlowPoint
                {
                    Name = point.Name,
                    OffsetMode = point.OffsetMode,
                    LocationX = point.LocationX,
                    LocationY = point.LocationY,
                    RatioX = point.RatioX,
                    RatioY = point.RatioY,
                    Flow = subFlow,
                };

                if (point.FlowAreaId != null && areaCopies.TryGetValue(point.FlowAreaId.Value, out FlowArea? anchor))
                    copy.FlowArea = anchor;

                dbContext.FlowPoints.Add(copy);
                pointCopies[point.Id] = copy;
            }

            await dbContext.SaveChangesAsync(ct);

            foreach (FlowStep step in moved)
            {
                if (step.FlowAreaId != null && areaCopies.TryGetValue(step.FlowAreaId.Value, out FlowArea? area))
                    step.FlowAreaId = area.Id;

                if (step.FlowPointId != null && pointCopies.TryGetValue(step.FlowPointId.Value, out FlowPoint? point))
                    step.FlowPointId = point.Id;

                if (step.FlowPointEndId != null && pointCopies.TryGetValue(step.FlowPointEndId.Value, out FlowPoint? end))
                    step.FlowPointEndId = end.Id;
            }
        }

        /// <summary>Closes the gap the extraction left, with the placeholder standing where it was.</summary>
        private static void RenumberSource(
            IReadOnlyList<FlowStep> steps,
            HashSet<int> moving,
            FlowStep placeholder,
            ExtractSubFlowDto dto)
        {
            List<FlowStep> siblings = steps
                .Where(x => !moving.Contains(x.Id))
                .Where(x => dto.SourceParentFlowStepId != null
                    ? x.ParentFlowStepId == dto.SourceParentFlowStepId
                    : x.ParentFlowStepId == null && x.FlowId == dto.SourceFlowId)
                .ToList();

            siblings.Add(placeholder);

            TreeStepMoveHelper.ApplyOrder(siblings, placeholder, dto.SourceOrderNumber);
        }
    }
}
