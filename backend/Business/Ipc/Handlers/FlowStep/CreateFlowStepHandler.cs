using AutoMapper;
using Business.Helpers;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepHandler : IRequestHandler<CreateFlowStepCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowStepCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep flowStep = _mapper.Map<FlowStep>(request.dto);
            flowStep.Id = 0;

            dbContext.FlowSteps.Add(flowStep);
            FlowStepImageSyncHelper.Sync(dbContext, flowStep, request.dto.FlowStepImages);
            AddBranchChildren(dbContext, flowStep);

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowStep.Id);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static void AddBranchChildren(AppDbContext dbContext, FlowStep flowStep)
        {
            if (!TreeStepHelper.HasBranchChildren(flowStep.FlowStepType))
                return;

            dbContext.FlowSteps.AddRange(
                NewBranch(flowStep, FlowStepTypeEnum.SUCCESS, "Success", 0),
                NewBranch(flowStep, FlowStepTypeEnum.FAILURE, "Failure", 1));
        }

        private static FlowStep NewBranch(FlowStep parent, FlowStepTypeEnum type, string name, int orderNumber) =>
            new FlowStep
            {
                ParentFlowStep = parent,
                FlowStepType = type,
                Name = name,
                OrderNumber = orderNumber,
                RootId = parent.RootId,
            };
    }
}
