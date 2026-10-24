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

            HashSet<string> taken = await TakenNamesAsync(dbContext, step.RootId, ct);
            step.Name = FlowNameHelper.MakeUnique(DesiredName(step), taken);

            dbContext.FlowSteps.Add(step);
            await SyncTemplatesAsync(dbContext, step, templates, ct);
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

            if (FlowStepNameHelper.IsNamedBySettings(step.FlowStepType))
            {
                HashSet<string> taken = await TakenNamesAsync(dbContext, step.RootId, step.Id, ct);
                step.Name = FlowNameHelper.MakeUnique(FlowStepNameHelper.NameOf(step), taken);
            }

            await SyncTemplatesAsync(dbContext, step, dto.FlowStepTemplates, ct);

            await dbContext.SaveChangesAsync(ct);
            await FlowTemplateDataService.RemoveUnusedAsync(dbContext, step.RootId, ct);

            return ResultDto<FlowStep>.Success(step);
        }
        /// <summary>
        /// Delete a FlowStep.
        /// </summary>
        public async Task<ResultDto<bool>> DeleteAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int? rootId = await dbContext.FlowSteps
                .Where(x => x.Id == id)
                .Select(x => (int?)x.RootId)
                .FirstOrDefaultAsync(ct);

            if (rootId == null)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            await dbContext.FlowSteps.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
            await FlowTemplateDataService.RemoveUnusedAsync(dbContext, rootId.Value, ct);

            return ResultDto<bool>.Success(true);
        }

        /// <summary>
        /// New steps saved at once from a recording, with the inputs they type. Only new rows are
        /// written: the flow's steps keep their ids and their history.
        /// </summary>
        public async Task<ResultDto<int>> CreateTreeAsync(int? flowId, int? parentFlowStepId, int targetIndex, IReadOnlyList<FlowStep> steps, IReadOnlyList<FlowCsvColumn> inputs, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            return await InsertTreeAsync(dbContext, flowId, parentFlowStepId, targetIndex, steps, inputs, ct);
        }

        /// <summary>
        /// A copy of a step and everything under it, placed at the target, with the id of the copy.
        /// A reference between the copied steps follows the copy; one to a step outside it is kept,
        /// and the validator says when the new place cannot reach it. Only into the flow it was
        /// copied from, whose areas, points and inputs it uses.
        /// </summary>
        public async Task<ResultDto<int>> PasteAsync(FlowStepPasteDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await DbContextFactory.CreateDbContextAsync(ct);

            int? sourceRootId = await dbContext.FlowSteps
                .Where(x => x.Id == dto.FlowStepId)
                .Select(x => (int?)x.RootId)
                .FirstOrDefaultAsync(ct);

            if (sourceRootId == null)
                return ResultDto<int>.Failure("The copied step no longer exists.");

            int? targetRootId = dto.TargetFlowId;
            if (dto.TargetParentFlowStepId != null)
            {
                targetRootId = await dbContext.FlowSteps
                    .Where(x => x.Id == dto.TargetParentFlowStepId)
                    .Select(x => (int?)x.RootId)
                    .FirstOrDefaultAsync(ct);
            }

            if (targetRootId != sourceRootId)
                return ResultDto<int>.Failure("A copy goes into the flow it was copied from: its areas, points and inputs are that flow's.");

            // Untracked, so each row read is a new object to insert rather than the saved one.
            List<FlowStep> flowSteps = await dbContext.FlowSteps
                .AsNoTracking()
                .Include(x => x.FlowStepTemplates)
                .Where(x => x.RootId == sourceRootId)
                .ToListAsync(ct);

            FlowStep source = flowSteps.First(x => x.Id == dto.FlowStepId);
            if (TreeStepHelper.IsBranchChild(source.FlowStepType))
                return ResultDto<int>.Failure("A Success or Failure branch belongs to its step, so it is copied with it.");

            List<FlowStep> copy = CopyOf(flowSteps, source);

            ResultDto<int> inserted = await InsertTreeAsync(dbContext, dto.TargetFlowId, dto.TargetParentFlowStepId, dto.TargetIndex, copy, [], ct);
            if (!inserted.IsSuccess)
                return inserted;

            return ResultDto<int>.Success(source.Id);
        }

        private static async Task<ResultDto<int>> InsertTreeAsync(AppDbContext dbContext, int? flowId, int? parentFlowStepId, int targetIndex, IReadOnlyList<FlowStep> steps, IReadOnlyList<FlowCsvColumn> inputs, CancellationToken ct)
        {
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
            HashSet<string> takenTemplates = await FlowTemplateDataService.TakenFileNamesAsync(dbContext, rootId.Value, ct);
            HashSet<FlowTemplate> pictures = new HashSet<FlowTemplate>();

            foreach (FlowStep step in rows)
                PrepareNewStep(step, rootId.Value, taken, takenTemplates, pictures);

            // The children of each new step in the order given, a check's branches Success then Failure.
            foreach (IGrouping<FlowStep, FlowStep> children in rows.Where(x => x.ParentFlowStep != null).GroupBy(x => x.ParentFlowStep!))
            {
                List<FlowStep> ordered = children.OrderBy(x => x.FlowStepType == FlowStepTypeEnum.FAILURE ? 1 : 0).ToList();

                for (int i = 0; i < ordered.Count; i++)
                    ordered[i].OrderNumber = i;
            }

            List<FlowStep> landing = rows.Where(x => x.ParentFlowStep == null).ToList();
            await PlaceAtTargetAsync(dbContext, landing, rootId.Value, parentFlowStepId, targetIndex, ct);

            foreach (FlowCsvColumn input in inputs)
            {
                input.Id = 0;
                input.FlowId = rootId.Value;
            }

            dbContext.FlowSteps.AddRange(rows);
            dbContext.FlowCsvColumns.AddRange(inputs);
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
        /// rows, so there is somewhere to add steps under it. The file names no step its settings
        /// name, so those are named here, apart from the names already taken.
        /// </summary>
        public static void AddTree(AppDbContext dbContext, Flow flow, IReadOnlyList<FlowStep> steps, HashSet<string> taken)
        {
            foreach (FlowStep step in steps)
            {
                step.RootId = flow.Id;
                if (step.ParentFlowStep == null)
                    step.FlowId = flow.Id;

                if (FlowStepNameHelper.IsNamedBySettings(step.FlowStepType))
                    step.Name = UniqueName(FlowStepNameHelper.NameOf(step), taken);
            }

            // The script names each picture once, so the steps that search for it share one row.
            foreach (FlowTemplate picture in steps.SelectMany(x => x.FlowStepTemplates).Select(x => x.FlowTemplate).Distinct())
                picture.FlowId = flow.Id;

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

        // A step's links to its pictures, matched by id and updated in place. A link names a picture
        // the flow has, or brings a new one. Editing a picture through one step edits it for every
        // step that links to it.
        private static async Task SyncTemplatesAsync(AppDbContext dbContext, FlowStep step, IEnumerable<FlowStepTemplateDto> dtos, CancellationToken ct)
        {
            List<FlowStepTemplateDto> wanted = dtos.ToList();
            List<FlowStepTemplate> existing = step.FlowStepTemplates.ToList();
            HashSet<int> keptIds = wanted.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowStepTemplate removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowStepTemplates.Remove(removed);

            // Only this flow's: a step cannot link to another flow's picture.
            HashSet<int> pictureIds = wanted.Where(x => x.FlowTemplateId > 0).Select(x => x.FlowTemplateId).ToHashSet();
            Dictionary<int, FlowTemplate> pictures = await dbContext.FlowTemplates
                .Where(x => x.FlowId == step.RootId && pictureIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);

            HashSet<string> takenTemplates = await FlowTemplateDataService.TakenFileNamesAsync(dbContext, step.RootId, ct);

            int order = 0;
            foreach (FlowStepTemplateDto dto in wanted)
            {
                FlowTemplate? picture = Picture(dbContext, step.RootId, dto, pictures, takenTemplates);
                if (picture == null)
                    continue;

                FlowStepTemplate? link = null;
                if (dto.Id > 0)
                    link = existing.FirstOrDefault(x => x.Id == dto.Id);

                if (link == null)
                {
                    link = new FlowStepTemplate { FlowStep = step };
                    dbContext.FlowStepTemplates.Add(link);
                }

                link.FlowTemplate = picture;
                link.OrderNumber = order++;
                link.IsRequired = dto.IsRequired;
                link.Accuracy = dto.Accuracy;
            }
        }

        // The flow's picture the link names, with what the form changed about it, or a new picture
        // given its file name here. Null for a picture the flow does not have.
        private static FlowTemplate? Picture(AppDbContext dbContext, int flowId, FlowStepTemplateDto dto, Dictionary<int, FlowTemplate> pictures, HashSet<string> takenTemplates)
        {
            FlowTemplate? picture;
            if (dto.FlowTemplateId > 0)
            {
                if (!pictures.TryGetValue(dto.FlowTemplateId, out picture))
                    return null;
            }
            else
            {
                if (dto.FlowTemplate == null)
                    return null;

                picture = new FlowTemplate { FlowId = flowId, Name = FlowTemplateDataService.NewName(takenTemplates) };
                dbContext.FlowTemplates.Add(picture);
            }

            if (dto.FlowTemplate != null)
                FlowTemplateDataService.CopyPicture(dto.FlowTemplate, picture);

            return picture;
        }


        // ================================================================
        // Private methods - a tree of new steps
        // ================================================================

        // A new row in the flow, named apart from everything in it, with the points it creates and
        // its templates in the order given. A branch row keeps its name: two Success rows are no clash.
        private static void PrepareNewStep(FlowStep step, int rootId, HashSet<string> taken, HashSet<string> takenTemplates, HashSet<FlowTemplate> pictures)
        {
            step.Id = 0;
            step.RootId = rootId;
            step.FlowId = null;
            step.ParentFlowStepId = null;

            if (!TreeStepHelper.IsBranchChild(step.FlowStepType))
                step.Name = UniqueName(DesiredName(step), taken);

            // A row already saved is linked by its id, or saving the new ones would insert it again.
            if (step.FlowArea != null && step.FlowArea.Id != 0)
            {
                step.FlowAreaId = step.FlowArea.Id;
                step.FlowArea = null;
            }

            // A recorded click knows where it happened, but a cursor step reads its position from a
            // FlowPoint, so the recording creates one for the step to link.
            foreach (FlowPoint? point in new[] { step.FlowPoint, step.FlowPointEnd })
            {
                if (point == null || point.Id != 0)
                    continue;

                point.FlowId = rootId;
                point.Name = UniqueName(point.Name, taken);

                if (point.FlowArea != null && point.FlowArea.Id != 0)
                {
                    point.FlowAreaId = point.FlowArea.Id;
                    point.FlowArea = null;
                }
            }

            int order = 0;
            foreach (FlowStepTemplate template in step.FlowStepTemplates)
            {
                template.Id = 0;
                template.OrderNumber = order++;
                PreparePicture(template, rootId, takenTemplates, pictures);
            }
        }

        // A picture already saved is linked by its id, as a pasted copy's is: the copy and the step
        // it came from share it. A new one, which several new steps may share, joins the flow once,
        // keeping the name it came with while the flow has none by it.
        private static void PreparePicture(FlowStepTemplate template, int rootId, HashSet<string> takenTemplates, HashSet<FlowTemplate> pictures)
        {
            FlowTemplate? picture = template.FlowTemplate;
            if (picture == null)
                return;

            if (picture.Id != 0)
            {
                template.FlowTemplateId = picture.Id;
                template.FlowTemplate = null!;
                return;
            }

            if (!pictures.Add(picture))
                return;

            picture.FlowId = rootId;
            if (picture.Name.Length == 0 || !takenTemplates.Add(picture.Name))
                picture.Name = FlowTemplateDataService.NewName(takenTemplates);
        }

        // The step and everything under it, in tree order, linked to each other rather than to the
        // rows they were read from: the rows are untracked, so they become the new ones.
        private static List<FlowStep> CopyOf(List<FlowStep> flowSteps, FlowStep top)
        {
            List<FlowStep> copy = new List<FlowStep>();
            AddWithChildren(flowSteps, top, copy);

            Dictionary<int, FlowStep> copiedById = copy.ToDictionary(x => x.Id);

            foreach (FlowStep step in copy)
            {
                if (step != top)
                    step.ParentFlowStep = copiedById[step.ParentFlowStepId!.Value];

                if (step.FlowStepReferenceId != null && copiedById.TryGetValue(step.FlowStepReferenceId.Value, out FlowStep? reference))
                {
                    step.FlowStepReference = reference;
                    step.FlowStepReferenceId = null;
                }

                if (step.FlowStepReferenceEndId != null && copiedById.TryGetValue(step.FlowStepReferenceEndId.Value, out FlowStep? referenceEnd))
                {
                    step.FlowStepReferenceEnd = referenceEnd;
                    step.FlowStepReferenceEndId = null;
                }

                foreach (FlowStepTemplate template in step.FlowStepTemplates)
                {
                    template.FlowStepId = 0;
                    template.FlowStep = step;
                }
            }

            return copy;
        }

        private static void AddWithChildren(List<FlowStep> flowSteps, FlowStep step, List<FlowStep> copy)
        {
            copy.Add(step);

            foreach (FlowStep child in flowSteps.Where(x => x.ParentFlowStepId == step.Id).OrderBy(x => x.OrderNumber))
                AddWithChildren(flowSteps, child, copy);
        }

        // What a step named by its settings is called, whatever name it came with.
        private static string DesiredName(FlowStep step)
        {
            if (FlowStepNameHelper.IsNamedBySettings(step.FlowStepType))
                return FlowStepNameHelper.NameOf(step);

            return step.Name;
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
