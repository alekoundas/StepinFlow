using Business.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepMovePreviewHandler : IRequestHandler<GetFlowStepMovePreviewQuery, ResultDto<FlowStepMovePreviewDto>>
    {
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepMovePreviewHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepMovePreviewDto>> Handle(GetFlowStepMovePreviewQuery request, CancellationToken ct)
        {
            FlowStepMoveDto dto = request.dto;

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep? moved = await dbContext.FlowSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.FlowStepId, ct);

            if (moved == null)
                return ResultDto<FlowStepMovePreviewDto>.Failure("Entity doesnt exist in the Database!");

            // One query for the whole tree, which is what RootId is for.
            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == moved.RootId)
                .ToListAsync(ct);

            string? error = FlowStepMoveHelper.Validate(steps, dto);
            if (error != null)
            {
                return ResultDto<FlowStepMovePreviewDto>.Success(new FlowStepMovePreviewDto
                {
                    IsValid = false,
                    ErrorMessage = error,
                    MovedStepName = moved.Name,
                });
            }

            FlowStep? movedInTree = steps.First(x => x.Id == dto.FlowStepId);

            string targetParentName = dto.TargetParentFlowStepId == null
                ? "the top level of the flow"
                : steps.First(x => x.Id == dto.TargetParentFlowStepId).Name;

            FlowStepMovePreviewDto preview = new FlowStepMovePreviewDto
            {
                IsValid = true,
                MovedStepName = movedInTree.Name,
                TargetParentName = targetParentName,
                MovedDescendantCount = FlowStepMoveHelper.GetDescendantIds(steps, dto.FlowStepId).Count,
                IsReorderOnly = movedInTree.ParentFlowStepId == dto.TargetParentFlowStepId,
                BrokenReferences = FlowStepMoveHelper.FindBrokenReferences(steps, dto),
            };

            return ResultDto<FlowStepMovePreviewDto>.Success(preview);
        }
    }
}
