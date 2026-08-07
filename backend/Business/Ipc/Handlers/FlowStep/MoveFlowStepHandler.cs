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
            string? error = FlowStepMoveHelper.Validate(steps, dto);
            if (error != null)
                return ResultDto<bool>.Failure(error);

            int? sourceParentFlowStepId = moved.ParentFlowStepId;
            int? sourceFlowId = moved.FlowId;
            bool parentChanged = sourceParentFlowStepId != dto.TargetParentFlowStepId;

            // A step is either a root step of the Flow or the child of another step, never both.
            moved.ParentFlowStepId = dto.TargetParentFlowStepId;
            moved.FlowId = dto.TargetParentFlowStepId == null ? dto.TargetFlowId : null;

            List<FlowStep> destinationSiblings = GetSiblings(steps, dto.TargetParentFlowStepId, dto.TargetFlowId);
            FlowStepMoveHelper.ApplyOrder(destinationSiblings, moved, dto.TargetIndex);

            if (parentChanged)
            {
                List<FlowStep> sourceSiblings = GetSiblings(steps, sourceParentFlowStepId, sourceFlowId);
                FlowStepMoveHelper.ApplyOrder(sourceSiblings, moved: null, targetIndex: 0);
            }

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static List<FlowStep> GetSiblings(List<FlowStep> steps, int? parentFlowStepId, int? flowId)
        {
            if (parentFlowStepId != null)
                return steps.Where(x => x.ParentFlowStepId == parentFlowStepId).ToList();

            return steps.Where(x => x.ParentFlowStepId == null && x.FlowId == flowId).ToList();
        }
    }
}
