using Business.Flows.FlowValidationService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Every change to a flow's steps, and to the templates and branch rows that belong to them.
    /// </summary>
    public sealed class FlowStepDataService : BaseDataService
    {
        public FlowStepDataService(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
            : base(dbContextFactory, flowValidationService)
        {
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Create a FlowStep, names must be unique and will be done so here.
        /// </summary>
        public async Task<ResultDto<int>> CreateAsync(FlowStep step, IEnumerable<FlowStepTemplateDto> templates, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            return await CreateAsync(dbContext, step, templates, ct);
        }

        /// <summary>
        /// Create a FlowStep, names must be unique and will be done so here.
        /// </summary>
        public static async Task<ResultDto<int>> CreateAsync(AppDbContext dbContext, FlowStep step, IEnumerable<FlowStepTemplateDto> templates, CancellationToken ct)
        {
            step.Id = 0;
            step.Name = FlowNameHelper.MakeUnique(step.Name, await TakenNamesAsync(dbContext, step.RootId, ct));

            dbContext.FlowSteps.Add(step);
            SyncTemplates(dbContext, step, templates);
            dbContext.FlowSteps.AddRange(TreeStepHelper.CreateBranchChildren(step));

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(step.Id);
        }

        /// <summary>
        /// Update a FlowStep.
        /// </summary>
        public async Task<ResultDto<FlowStep>> UpdateAsync(FlowStepDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowStep? step = await dbContext.FlowSteps
                .Include(x => x.FlowStepTemplates)
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (step == null)
                return ResultDto<FlowStep>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(step).CurrentValues.SetValues(dto);
            SyncTemplates(dbContext, step, dto.FlowStepTemplates);

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<FlowStep>.Success(step);
        }
        /// <summary>
        /// Delete a FlowStep.
        /// </summary>
        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int count = await dbContext.FlowSteps.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            if (count <= 0)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            return ResultDto<bool>.Success(true);
        }

        /// <summary>
        /// New steps saved at once from a recording.
        /// </summary>
        public async Task<ResultDto<int>> CreateTreeAsync(int? flowId, int? parentFlowStepId, int targetIndex, IReadOnlyList<FlowStep> steps, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int? rootId = flowId;
            if (parentFlowStepId != null)
            {
                FlowStep? parent = await dbContext.FlowSteps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parentFlowStepId, ct);
                if (parent == null)
                    return ResultDto<int>.Failure("The step to save under no longer exists.");

                if (!TreeStepHelper.CanContainChildren(parent.FlowStepType))
                    return ResultDto<int>.Failure($"\"{parent.Name}\" holds steps in its branches, not directly.");

                rootId = parent.RootId;
            }

            // Check the flow always exists by now.
            if (rootId == null || !await dbContext.Flows.AnyAsync(x => x.Id == rootId, ct))
                return ResultDto<int>.Failure("The flow to save into no longer exists.");

            // A check the steps hang nothing under still gets both branch rows.
            List<FlowStep> rows = steps.ToList();
            foreach (FlowStep step in steps.Where(x => TreeStepHelper.HasBranchChildren(x.FlowStepType)))
                rows.AddRange(MissingBranches(step, steps));

            // Grows as the steps add to it, so the twelfth is unique against the eleven before it as
            // well as against what was already saved.
            HashSet<string> taken = await TakenNamesAsync(dbContext, rootId.Value, ct);

            foreach (FlowStep step in rows)
                PrepareNewStep(step, rootId.Value, taken);

            // The children of each new step in the order given, a check's branches Success then Failure.
            foreach (IGrouping<FlowStep, FlowStep> children in rows.Where(x => x.ParentFlowStep != null).GroupBy(x => x.ParentFlowStep!))
            {
                List<FlowStep> ordered = children.OrderBy(x => x.FlowStepType == FlowStepTypeEnum.FAILURE ? 1 : 0).ToList();

                for (int i = 0; i < ordered.Count; i++)
                    ordered[i].OrderNumber = i;
            }

            List<FlowStep> landing = rows.Where(x => x.ParentFlowStep == null).ToList();
            await PlaceAtTargetAsync(dbContext, landing, rootId.Value, parentFlowStepId, targetIndex, ct);

            dbContext.FlowSteps.AddRange(rows);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(rootId.Value);
        }



        /// <summary>
        /// UI Tree drag and drop a FlowStep.
        /// Re-validated because the tree may have changed between the drop and the confirmation.
        /// </summary>
        public async Task<ResultDto<bool>> MoveAsync(FlowStepMoveDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            FlowStep? moved = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == dto.FlowStepId, ct);
            if (moved == null)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            // Tracked, because both sibling lists get renumbered.
            List<FlowStep> steps = await dbContext.FlowSteps.Where(x => x.RootId == moved.RootId).ToListAsync(ct);

            string? error = TreeStepMoveHelper.Validate(steps, dto);
            if (error != null)
                return ResultDto<bool>.Failure(error);

            // Computed before anything moves, because it compares the chain before against after.
            List<FlowStepBrokenReferenceDto> brokenReferences = TreeStepMoveHelper.FindBrokenReferences(steps, dto);

            int? sourceParentFlowStepId = moved.ParentFlowStepId;
            int? sourceFlowId = moved.FlowId;
            bool parentChanged = sourceParentFlowStepId != dto.TargetParentFlowStepId;

            // A step is either a root step of the Flow or the child of another step, never both.
            moved.ParentFlowStepId = dto.TargetParentFlowStepId;
            moved.FlowId = null;
            if (dto.TargetParentFlowStepId == null)
                moved.FlowId = dto.TargetFlowId;

            TreeStepMoveHelper.ApplyOrder(Siblings(steps, dto.TargetParentFlowStepId, dto.TargetFlowId), moved, dto.TargetIndex);

            if (parentChanged)
                TreeStepMoveHelper.ApplyOrder(Siblings(steps, sourceParentFlowStepId, sourceFlowId), moved: null, targetIndex: 0);

            ClearBrokenReferences(steps, brokenReferences);

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }

 

        /// <summary>
        /// Steps read from a script, linked to their parents, references, areas, points and
        /// templates. A check the file wrote with an empty branch left out still gets both branch
        /// rows, so there is somewhere to add steps under it.
        /// </summary>
        public static void AddTree(AppDbContext dbContext, Flow flow, IReadOnlyList<FlowStep> steps)
        {
            foreach (FlowStep step in steps)
            {
                step.RootId = flow.Id;
                if (step.ParentFlowStep == null)
                    step.FlowId = flow.Id;
            }

            dbContext.FlowSteps.AddRange(steps);

            foreach (FlowStep step in steps.Where(x => TreeStepHelper.HasBranchChildren(x.FlowStepType)))
                dbContext.FlowSteps.AddRange(MissingBranches(step, steps));
        }


        // ================================================================
        // Private methods - branch rows and templates
        // ================================================================

        // Success then Failure, as a step made in a form has them, keeping whichever the file wrote.
        // Both written keeps the file's order.
        private static List<FlowStep> MissingBranches(FlowStep step, IReadOnlyList<FlowStep> steps)
        {
            List<FlowStep> written = steps.Where(x => x.ParentFlowStep == step).ToList();
            if (written.Any(x => x.FlowStepType == FlowStepTypeEnum.SUCCESS) && written.Any(x => x.FlowStepType == FlowStepTypeEnum.FAILURE))
                return [];

            List<FlowStep> branches = TreeStepHelper.CreateBranchChildren(step)
                .Select(x => written.FirstOrDefault(y => y.FlowStepType == x.FlowStepType) ?? x)
                .ToList();

            for (int i = 0; i < branches.Count; i++)
                branches[i].OrderNumber = i;

            return branches.Except(written).ToList();
        }

        // Templates are edited as part of their step, so they are matched by id and updated in
        // place rather than replaced.
        private static void SyncTemplates(AppDbContext dbContext, FlowStep step, IEnumerable<FlowStepTemplateDto> dtos)
        {
            List<FlowStepTemplate> existing = step.FlowStepTemplates.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowStepTemplate removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowStepTemplates.Remove(removed);

            int order = 0;
            foreach (FlowStepTemplateDto dto in dtos)
            {
                FlowStepTemplate? image = null;
                if (dto.Id > 0)
                    image = existing.FirstOrDefault(x => x.Id == dto.Id);

                if (image == null)
                {
                    image = new FlowStepTemplate { FlowStep = step };
                    dbContext.FlowStepTemplates.Add(image);
                }

                image.Name = dto.Name;
                image.OrderNumber = order++;
                image.IsRequired = dto.IsRequired;

                // Only overwrite the blob when the client actually sent one: the list view sends
                // templates back without their bytes so a save does not push megabytes per step.
                if (dto.TemplateImage != null && dto.TemplateImage.Length > 0)
                    image.TemplateImage = dto.TemplateImage;

                image.Accuracy = dto.Accuracy;

                image.ClickOffsetX = dto.ClickOffsetX;
                image.ClickOffsetY = dto.ClickOffsetY;

                image.AuthoredFlowAreaWidth = dto.AuthoredFlowAreaWidth;
                image.AuthoredFlowAreaHeight = dto.AuthoredFlowAreaHeight;
                image.AuthoredDpi = dto.AuthoredDpi;
            }
        }


        // ================================================================
        // Private methods - a tree of new steps
        // ================================================================

        // A new row in the flow, named apart from everything in it, with the points it creates and
        // its templates in the order given. A branch row keeps its name: two Success rows are no clash.
        private static void PrepareNewStep(FlowStep step, int rootId, HashSet<string> taken)
        {
            step.Id = 0;
            step.RootId = rootId;
            step.FlowId = null;
            step.ParentFlowStepId = null;

            if (!TreeStepHelper.IsBranchChild(step.FlowStepType))
                step.Name = UniqueName(step.Name, taken);

            // A recorded click knows where it happened, but a cursor step reads its position from a
            // FlowPoint, so the recording creates one for the step to link.
            foreach (FlowPoint? point in new[] { step.FlowPoint, step.FlowPointEnd })
            {
                if (point == null || point.Id != 0)
                    continue;

                point.FlowId = rootId;
                point.Name = UniqueName(point.Name, taken);
            }

            int order = 0;
            foreach (FlowStepTemplate template in step.FlowStepTemplates)
            {
                template.Id = 0;
                template.OrderNumber = order++;
            }
        }

        private static string UniqueName(string desired, HashSet<string> taken)
        {
            string name = FlowNameHelper.MakeUnique(desired, taken);
            taken.Add(name);

            return name;
        }


        // The steps with no parent among the new ones slot in at the index and the siblings around
        // them are renumbered - the ordering a drag and drop uses.
        private static async Task PlaceAtTargetAsync(AppDbContext dbContext, List<FlowStep> landing, int flowId, int? parentFlowStepId, int targetIndex, CancellationToken ct)
        {
            List<FlowStep> siblings;
            if (parentFlowStepId != null)
                siblings = await dbContext.FlowSteps.Where(x => x.ParentFlowStepId == parentFlowStepId).OrderBy(x => x.OrderNumber).ToListAsync(ct);
            else
                siblings = await dbContext.FlowSteps.Where(x => x.ParentFlowStepId == null && x.FlowId == flowId).OrderBy(x => x.OrderNumber).ToListAsync(ct);

            foreach (FlowStep step in landing)
            {
                if (parentFlowStepId != null)
                    step.ParentFlowStepId = parentFlowStepId;
                else
                    step.FlowId = flowId;
            }

            siblings.InsertRange(Math.Clamp(targetIndex, 0, siblings.Count), landing);

            for (int i = 0; i < siblings.Count; i++)
                siblings[i].OrderNumber = i;
        }

        private static List<FlowStep> Siblings(List<FlowStep> steps, int? parentFlowStepId, int? flowId)
        {
            if (parentFlowStepId != null)
                return steps.Where(x => x.ParentFlowStepId == parentFlowStepId).ToList();

            return steps.Where(x => x.ParentFlowStepId == null && x.FlowId == flowId).ToList();
        }
    }
}
