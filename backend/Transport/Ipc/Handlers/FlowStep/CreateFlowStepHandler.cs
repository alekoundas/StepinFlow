using AutoMapper;
using Business.Flows;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowStepHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> HandleAsync(FlowStepDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep flowStep = _mapper.Map<FlowStep>(dto);
            flowStep.Id = 0;

            // Made unique here rather than argued about at export: the script refers to a step by
            // name, so two steps called the same thing make the reference ambiguous.
            HashSet<string> taken = await FlowNameLookup.TakenAsync(dbContext, flowStep.RootId, ct);
            flowStep.Name = FlowNameHelper.MakeUnique(flowStep.Name, taken);

            dbContext.FlowSteps.Add(flowStep);
            FlowStepTemplateSync.Sync(dbContext, flowStep, dto.FlowStepTemplates);
            dbContext.FlowSteps.AddRange(TreeStepHelper.CreateBranchChildren(flowStep));

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowStep.Id);
        }

    }
}
