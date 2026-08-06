using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowStepImageHandler : IRequestHandler<GetFlowStepImageQuery, ResultDto<FlowStepImageDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepImageHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepImageDto>> Handle(GetFlowStepImageQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            FlowStepImage? flowStepImage = await dbContext.FlowStepImages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.id, ct);

            if (flowStepImage == null)
                return ResultDto<FlowStepImageDto>.Failure("Entity doesnt exist in the Database!");

            FlowStepImageDto? flowStepImageDto = _mapper.Map<FlowStepImageDto>(flowStepImage);
            return ResultDto<FlowStepImageDto>.Success(flowStepImageDto);
        }
    }
}
