using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowHandler : IRequestHandler<GetLazyFlowQuery, ResultDto<LazyResponseDto<FlowDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowDto>>> Handle(GetLazyFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            IQueryable<Flow> query = dbContext.Flows.AsNoTracking();

            // Unset lists everything, which is what a lookup wants; the two pages always set it.
            if (request.dto.IsSubFlow is bool isSubFlow)
                query = query.Where(x => x.IsSubFlow == isSubFlow);

            List<Flow> flows = await query.OrderBy(x => x.OrderNumber).ToListAsync(ct);

            List<FlowDto> flowDtos = _mapper.Map<List<FlowDto>>(flows);
            LazyResponseDto<FlowDto> dataTableResponseDto = new LazyResponseDto<FlowDto>();
            dataTableResponseDto.Data = flowDtos;
            dataTableResponseDto.TotalRecords = flowDtos.Count;

            return ResultDto<LazyResponseDto<FlowDto>>.Success(dataTableResponseDto);
        }
    }
}
