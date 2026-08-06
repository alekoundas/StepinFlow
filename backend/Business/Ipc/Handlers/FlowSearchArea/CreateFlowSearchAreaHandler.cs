using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowSearchAreaHandler : IRequestHandler<CreateFlowSearchAreaCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowSearchAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowSearchAreaCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowSearchArea flowSearchArea = _mapper.Map<FlowSearchArea>(request.dto);
            flowSearchArea.Id = 0;

            dbContext.FlowSearchAreas.Add(flowSearchArea);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowSearchArea.Id);
        }
    }
}
