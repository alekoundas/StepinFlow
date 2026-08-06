using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetSubFlowHandler : IRequestHandler<GetSubFlowQuery, ResultDto<SubFlowDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetSubFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<SubFlowDto>> Handle(GetSubFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            SubFlow? subFlow = await dbContext.SubFlows
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.id, ct);

            if (subFlow == null)
                return ResultDto<SubFlowDto>.Failure("Entity doesnt exist in the Database!");

            SubFlowDto? subFlowDto = _mapper.Map<SubFlowDto>(subFlow);
            return ResultDto<SubFlowDto>.Success(subFlowDto);
        }
    }
}
