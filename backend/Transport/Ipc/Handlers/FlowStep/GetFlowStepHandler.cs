using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowStepHandler : IRequestHandler<GetFlowStepQuery, ResultDto<FlowStepDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepDto>> Handle(GetFlowStepQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            FlowStep? flowStep = await dbContext.FlowSteps
                .AsNoTracking()
                .Include(x => x.FlowStepTemplates.OrderBy(image => image.OrderNumber))
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (flowStep == null)
                return ResultDto<FlowStepDto>.Failure("Entity doesnt exist in the Database!");

            FlowStepDto? flowStepDto = _mapper.Map<FlowStepDto>(flowStep);
            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
