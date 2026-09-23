using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Transport.Messages;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowStepTemplateHandler : IRequestHandler<GetFlowStepTemplateQuery, ResultDto<FlowStepTemplateDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepTemplateHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepTemplateDto>> Handle(GetFlowStepTemplateQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            FlowStepTemplate? flowStepTemplate = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (flowStepTemplate == null)
                return ResultDto<FlowStepTemplateDto>.Failure("Entity doesnt exist in the Database!");

            FlowStepTemplateDto? flowStepTemplateDto = _mapper.Map<FlowStepTemplateDto>(flowStepTemplate);
            return ResultDto<FlowStepTemplateDto>.Success(flowStepTemplateDto);
        }
    }
}
