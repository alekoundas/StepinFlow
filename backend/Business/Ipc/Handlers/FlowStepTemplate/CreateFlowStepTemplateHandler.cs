using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepTemplateHandler : IRequestHandler<CreateFlowStepTemplateCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepTemplateHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowStepTemplateCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStepTemplate flowStepTemplate = _mapper.Map<FlowStepTemplate>(request.dto);
            flowStepTemplate.Id = 0;

            dbContext.FlowStepTemplates.Add(flowStepTemplate);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowStepTemplate.Id);
        }
    }
}
