using Business.FlowScript.Models;
using Business.Flows.FlowValidationService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow as a whole: the flow row, its sizes and inputs, and the writes that
    /// span all of its tables - an import, an extraction into a sub-flow.
    /// </summary>
    public sealed class FlowDataService : BaseDataService
    {
        public FlowDataService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IFlowValidationService flowValidationService) : base(dbContextFactory, flowValidationService)
        {
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<int>> CreateAsync(Flow flow, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            flow.Id = 0;

            dbContext.Flows.Add(flow);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flow.Id);
        }

        /// <summary>
        /// The flow form: the flow's own fields, and its areas, points and sizes as the form left
        /// them. Areas first, because a point can sit in an area created in this same save.
        /// </summary>
        public async Task<ResultDto<Flow>> UpdateAsync(FlowDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            Flow? flow = await dbContext.Flows
                .Include(x => x.FlowAreas)
                .Include(x => x.FlowPoints)
                .Include(x => x.FlowViewports)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (flow == null)
                return ResultDto<Flow>.Failure("Flow not found");

            flow.Name = dto.Name;
            flow.Description = dto.Description;

            Dictionary<int, FlowArea> areasByDtoId = SyncAreas(dbContext, flow, dto.FlowAreas);
            SyncPoints(dbContext, flow, dto.FlowPoints, areasByDtoId);
            SyncViewports(dbContext, flow, dto.FlowViewports);

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<Flow>.Success(flow);
        }

        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.Flows.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }

        /// <summary>
        /// Marks a flow as callable. One way on purpose: a caller's SubFlowId can then never point
        /// at something that has stopped being invokable, which is what removes every stale caller
        /// case from the rest of the feature.
        /// </summary>
        public async Task<ResultDto<bool>> PromoteToSubFlowAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            Flow? flow = await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (flow == null)
                return ResultDto<bool>.Failure("That flow no longer exists.");

            flow.IsSubFlow = true;
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }

        // Saves more than once, so in a transaction: an import that fails part way leaves the flow
        // as it was.
        public async Task<ResultDto<(int FlowId, FlowValidationResultDto Validation)>> ReplaceAsync(FlowScriptSchema schema, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            ResultDto<(int FlowId, FlowValidationResultDto Validation)> replaced = await ReplaceAsync(dbContext, schema, ct);
            await transaction.CommitAsync(ct);

            return replaced;
        }

        /// <summary>
        /// The import's write path: the flow's rows swapped for the linked rows a script reads into,
        /// saved as one graph, then validated. The flow row itself stays, for its execution history,
        /// and a flow with errors is saved and shows them.
        /// </summary>
        public async Task<ResultDto<(int FlowId, FlowValidationResultDto Validation)>> ReplaceAsync(AppDbContext dbContext, FlowScriptSchema schema, CancellationToken ct)
        {
            Flow? existing = await dbContext.Flows.FirstOrDefaultAsync(x => x.PublicId == schema.Flow.PublicId, ct);

            // Saved on its own first: RootId names the flow by id, with no link EF could fill it from.
            Flow flow = existing ?? schema.Flow;
            flow.Name = schema.Flow.Name;

            if (existing == null)
                dbContext.Flows.Add(flow);

            await dbContext.SaveChangesAsync(ct);

            if (existing != null)
            {
                await dbContext.FlowSteps.Where(x => x.RootId == flow.Id).ExecuteDeleteAsync(ct);
                await dbContext.FlowPoints.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                await dbContext.FlowAreas.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                await dbContext.FlowCsvColumns.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
                await dbContext.FlowViewports.Where(x => x.FlowId == flow.Id).ExecuteDeleteAsync(ct);
            }

            foreach (FlowCsvColumn input in schema.Inputs)
                input.FlowId = flow.Id;

            foreach (FlowViewport viewport in schema.Viewports)
                viewport.FlowId = flow.Id;

            dbContext.FlowCsvColumns.AddRange(schema.Inputs);
            dbContext.FlowViewports.AddRange(schema.Viewports);
            FlowAreaDataService.Add(dbContext, flow, schema.Areas);
            FlowPointDataService.Add(dbContext, flow, schema.Points);
            FlowStepDataService.AddTree(dbContext, flow, schema.Steps);

            // One save for the whole graph: EF inserts in dependency order and fills every key from
            // the links.
            await dbContext.SaveChangesAsync(ct);

            FlowValidationResultDto validation = await ValidateAsync(dbContext, flow.Id, ct);

            return ResultDto<(int FlowId, FlowValidationResultDto Validation)>.Success((flow.Id, validation));
        }

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
        ///
        /// Saves more than once, so in a transaction. Every refusal comes before the first save.
        /// </summary>
        public async Task<ResultDto<ExtractSubFlowResultDto>> ExtractSubFlowAsync(ExtractSubFlowDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            FlowStep? head = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == dto.FlowStepId, ct);
            if (head == null)
                return ResultDto<ExtractSubFlowResultDto>.Failure("That step no longer exists.");

            if (TreeStepHelper.IsBranchChild(head.FlowStepType))
                return ResultDto<ExtractSubFlowResultDto>.Failure("Success and Failure belong to the step above them and cannot be extracted on their own.");

            List<FlowStep> steps = await dbContext.FlowSteps
                .Where(x => x.RootId == head.RootId)
                .ToListAsync(ct);

            HashSet<int> moving = TreeStepMoveHelper.GetDescendantIds(steps, head.Id);
            moving.Add(head.Id);

            // Validate that a step doesnt reference a step outside of the subflow.
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

            // Saved before the placeholder is named, so the steps that left are not in its way.
            await dbContext.SaveChangesAsync(ct);

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

            await FlowStepDataService.CreateAsync(dbContext, placeholder, [], ct);

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


        // ================================================================
        // Private methods - the flow form
        // ================================================================

        // Order is what the matrix runs in, so it comes from the list rather than from the ids.
        private static void SyncViewports(AppDbContext dbContext, Flow flow, IEnumerable<FlowViewportDto> dtos)
        {
            List<FlowViewport> existing = flow.FlowViewports.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowViewport removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowViewports.Remove(removed);

            int order = 0;

            foreach (FlowViewportDto dto in dtos)
            {
                FlowViewport? viewport = null;
                if (dto.Id > 0)
                    viewport = existing.FirstOrDefault(x => x.Id == dto.Id);

                if (viewport == null)
                {
                    viewport = new FlowViewport { FlowId = flow.Id };
                    dbContext.FlowViewports.Add(viewport);
                }

                viewport.Width = dto.Width;
                viewport.Height = dto.Height;
                viewport.OrderNumber = order++;
            }
        }

        private static Dictionary<int, FlowArea> SyncAreas(AppDbContext dbContext, Flow flow, IEnumerable<FlowAreaDto> dtos)
        {
            List<FlowArea> existing = flow.FlowAreas.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowArea removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowAreas.Remove(removed);

            Dictionary<int, FlowArea> byDtoId = new Dictionary<int, FlowArea>();

            foreach (FlowAreaDto dto in dtos)
            {
                FlowArea? area = null;
                if (dto.Id > 0)
                    area = existing.FirstOrDefault(x => x.Id == dto.Id);

                if (area == null)
                {
                    area = new FlowArea { FlowId = flow.Id };
                    dbContext.FlowAreas.Add(area);
                }

                area.Name = dto.Name;
                area.Type = dto.Type;

                area.SizingMode = dto.SizingMode;
                area.LocationX = dto.LocationX;
                area.LocationY = dto.LocationY;
                area.Width = dto.Width;
                area.Height = dto.Height;
                area.RatioX = dto.RatioX;
                area.RatioY = dto.RatioY;
                area.RatioWidth = dto.RatioWidth;
                area.RatioHeight = dto.RatioHeight;

                area.ProcessName = dto.ProcessName;
                area.TitlePattern = dto.TitlePattern;
                area.TitleMatchMode = dto.TitleMatchMode;
                area.UseClientArea = dto.UseClientArea;

                area.TabMatchValue = dto.TabMatchValue;
                area.TabMatchOn = dto.TabMatchOn;

                area.MonitorDeviceName = dto.MonitorDeviceName;

                area.ScalesWith = dto.ScalesWith;
                area.AuthoredDpi = dto.AuthoredDpi;

                byDtoId[dto.Id] = area;
            }

            foreach (FlowAreaDto dto in dtos)
            {
                FlowArea area = byDtoId[dto.Id];

                area.ParentFlowArea = null;
                if (dto.ParentFlowAreaId != null && byDtoId.TryGetValue(dto.ParentFlowAreaId.Value, out FlowArea? parent) && parent != area)
                    area.ParentFlowArea = parent;

                if (area.ParentFlowArea == null)
                    area.ParentFlowAreaId = null;
            }

            return byDtoId;
        }

        private static void SyncPoints(AppDbContext dbContext, Flow flow, IEnumerable<FlowPointDto> dtos, Dictionary<int, FlowArea> areasByDtoId)
        {
            List<FlowPoint> existing = flow.FlowPoints.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowPoint removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowPoints.Remove(removed);

            foreach (FlowPointDto dto in dtos)
            {
                FlowPoint? point = null;
                if (dto.Id > 0)
                    point = existing.FirstOrDefault(x => x.Id == dto.Id);

                if (point == null)
                {
                    point = new FlowPoint { FlowId = flow.Id };
                    dbContext.FlowPoints.Add(point);
                }

                point.Name = dto.Name;
                point.OffsetMode = dto.OffsetMode;
                point.LocationX = dto.LocationX;
                point.LocationY = dto.LocationY;
                point.AuthoredDpi = dto.AuthoredDpi;
                point.RatioX = dto.RatioX;
                point.RatioY = dto.RatioY;

                point.FlowArea = null;
                if (dto.FlowAreaId != null && areasByDtoId.TryGetValue(dto.FlowAreaId.Value, out FlowArea? area))
                    point.FlowArea = area;

                if (point.FlowArea == null)
                    point.FlowAreaId = null;
            }
        }


        // ================================================================
        // Private methods - extraction
        // ================================================================

        // A step referencing a step outside the subflow is not allowed. 
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

                    string where = "is being extracted";
                    if (moving.Contains(step.Id))
                        where = "is staying behind";

                    return $"\"{step.Name}\" reads the result of \"{nameById[target]}\", which {where}. Move it in, or point that step somewhere else, then extract again.";
                }
            }

            return null;
        }

        // Areas and points belong to a flow, so the moved steps would keep pointing at ones the
        // sub-flow cannot list or edit. Copied rather than shared: a sub-flow meant to be reused
        // should not change when the flow it came from is edited.
        //
        // A copy is the row itself, loaded untracked with its id cleared, so no column can be left
        // behind. The copies are linked to each other and the moved steps to the copies, and the
        // next save turns the links into keys.
        private static async Task CopyAreasAndPointsAsync(AppDbContext dbContext, Flow subFlow, List<FlowStep> moved, CancellationToken ct)
        {
            HashSet<int> pointIds = moved
                .SelectMany(x => new[] { x.FlowPointId, x.FlowPointEndId })
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToHashSet();

            List<FlowPoint> points = await dbContext.FlowPoints.AsNoTracking().Where(x => pointIds.Contains(x.Id)).ToListAsync(ct);

            // The areas the steps search in, and the ones the points are measured from.
            HashSet<int> areaIds = moved.Select(x => x.FlowAreaId)
                .Concat(points.Select(x => x.FlowAreaId))
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToHashSet();

            List<FlowArea> areas = await AreasWithParentsAsync(dbContext, areaIds, ct);

            Dictionary<int, FlowArea> areaCopies = areas.ToDictionary(x => x.Id);
            foreach (FlowArea copy in areas)
            {
                int? parentId = copy.ParentFlowAreaId;

                copy.Id = 0;
                copy.FlowId = subFlow.Id;
                copy.UpdatedOn = null;
                copy.ParentFlowAreaId = null;

                if (parentId != null)
                    copy.ParentFlowArea = areaCopies[parentId.Value];
            }

            Dictionary<int, FlowPoint> pointCopies = points.ToDictionary(x => x.Id);
            foreach (FlowPoint copy in points)
            {
                int? areaId = copy.FlowAreaId;

                copy.Id = 0;
                copy.FlowId = subFlow.Id;
                copy.UpdatedOn = null;
                copy.FlowAreaId = null;

                if (areaId != null)
                    copy.FlowArea = areaCopies[areaId.Value];
            }

            dbContext.FlowAreas.AddRange(areas);
            dbContext.FlowPoints.AddRange(points);

            foreach (FlowStep step in moved)
            {
                if (step.FlowAreaId != null)
                    step.FlowArea = areaCopies[step.FlowAreaId.Value];

                if (step.FlowPointId != null)
                    step.FlowPoint = pointCopies[step.FlowPointId.Value];

                if (step.FlowPointEndId != null)
                    step.FlowPointEnd = pointCopies[step.FlowPointEndId.Value];
            }
        }

        // The areas asked for and every area above them, however deep, so each copy has its
        // parent's copy to sit in.
        private static async Task<List<FlowArea>> AreasWithParentsAsync(AppDbContext dbContext, HashSet<int> areaIds, CancellationToken ct)
        {
            List<FlowArea> areas = new List<FlowArea>();
            HashSet<int> pending = areaIds;

            while (pending.Count > 0)
            {
                List<FlowArea> loaded = await dbContext.FlowAreas.AsNoTracking().Where(x => pending.Contains(x.Id)).ToListAsync(ct);
                areas.AddRange(loaded);

                pending = loaded
                    .Where(x => x.ParentFlowAreaId != null && areas.All(y => y.Id != x.ParentFlowAreaId))
                    .Select(x => x.ParentFlowAreaId!.Value)
                    .ToHashSet();
            }

            return areas;
        }

        // Closes the gap the extraction left, with the placeholder standing where it was.
        private static void RenumberSource(IReadOnlyList<FlowStep> steps, HashSet<int> moving, FlowStep placeholder, ExtractSubFlowDto dto)
        {
            List<FlowStep> siblings;
            if (dto.SourceParentFlowStepId != null)
                siblings = steps.Where(x => !moving.Contains(x.Id) && x.ParentFlowStepId == dto.SourceParentFlowStepId).ToList();
            else
                siblings = steps.Where(x => !moving.Contains(x.Id) && x.ParentFlowStepId == null && x.FlowId == dto.SourceFlowId).ToList();

            siblings.Add(placeholder);

            TreeStepMoveHelper.ApplyOrder(siblings, placeholder, dto.SourceOrderNumber);
        }
    }
}
