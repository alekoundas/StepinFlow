using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateFlowStepTemplateHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepTemplateHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> HandleAsync(FlowStepTemplateDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStepTemplate flowStepTemplate = _mapper.Map<FlowStepTemplate>(dto);
            flowStepTemplate.Id = 0;

            dbContext.FlowStepTemplates.Add(flowStepTemplate);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowStepTemplate.Id);
        }
    }
}
