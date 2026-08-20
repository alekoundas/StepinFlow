using Business.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class MoveFlowStepHandler : IRequestHandler<MoveFlowStepCommand, ResultDto<bool>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public MoveFlowStepHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(MoveFlowStepCommand request, CancellationToken ct)
        {
            FlowStepMoveDto dto = request.dto;

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep? moved = await dbContext.FlowSteps.FirstOrDefaultAsync(x => x.Id == dto.FlowStepId, ct);
            if (moved == null)
                return ResultDto<bool>.Failure("Entity doesnt exist in the Database!");

            // Tracked, because both sibling lists get renumbered.
            List<FlowStep> steps = await dbContext.FlowSteps
                .Where(x => x.RootId == moved.RootId)
                .ToListAsync(ct);

            // Re-validated here and not trusted from the preview: the tree may have changed
            // between the drop and the confirmation.
            string? error = TreeStepMoveHelper.Validate(steps, dto);
            if (error != null)
                return ResultDto<bool>.Failure(error);

            // Computed before anything moves, because it compares the chain before against after.
            // The preview only warned about these; clearing them is what makes the warning true.
            List<FlowStepBrokenReferenceDto> brokenReferences = TreeStepMoveHelper.FindBrokenReferences(steps, dto);

            int? sourceParentFlowStepId = moved.ParentFlowStepId;
            int? sourceFlowId = moved.FlowId;
            bool parentChanged = sourceParentFlowStepId != dto.TargetParentFlowStepId;

            // A step is either a root step of the Flow or the child of another step, never both.
            moved.ParentFlowStepId = dto.TargetParentFlowStepId;
            moved.FlowId = dto.TargetParentFlowStepId == null ? dto.TargetFlowId : null;

            List<FlowStep> destinationSiblings = GetSiblings(steps, dto.TargetParentFlowStepId, dto.TargetFlowId);
            TreeStepMoveHelper.ApplyOrder(destinationSiblings, moved, dto.TargetIndex);

            if (parentChanged)
            {
                List<FlowStep> sourceSiblings = GetSiblings(steps, sourceParentFlowStepId, sourceFlowId);
                TreeStepMoveHelper.ApplyOrder(sourceSiblings, moved: null, targetIndex: 0);
            }

            ClearBrokenReferences(steps, brokenReferences);

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// A step that no longer runs under the one it reads would otherwise keep the reference
        /// and act on whatever that step last produced. Cleared rather than repointed: the step now
        /// needs a source the user has to choose, and an empty required dropdown says so where a
        /// silently wrong value would not.
        /// </summary>
        private static void ClearBrokenReferences(
            List<FlowStep> steps,
            List<FlowStepBrokenReferenceDto> brokenReferences)
        {
            foreach (FlowStepBrokenReferenceDto reference in brokenReferences)
            {
                FlowStep? step = steps.FirstOrDefault(x => x.Id == reference.FlowStepId);
                if (step == null)
                    continue;

                if (reference.IsEndReference)
                    step.FlowStepReferenceEndId = null;
                else
                    step.FlowStepReferenceId = null;
            }
        }

        private static List<FlowStep> GetSiblings(List<FlowStep> steps, int? parentFlowStepId, int? flowId)
        {
            if (parentFlowStepId != null)
                return steps.Where(x => x.ParentFlowStepId == parentFlowStepId).ToList();

            return steps.Where(x => x.ParentFlowStepId == null && x.FlowId == flowId).ToList();
        }
    }
}
