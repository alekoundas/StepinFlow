using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowLocationHandler : IRequestHandler<CreateFlowLocationCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowLocationHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowLocationCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowLocation flowLocation = _mapper.Map<FlowLocation>(request.dto);
            flowLocation.Id = 0;

            dbContext.FlowLocations.Add(flowLocation);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowLocation.Id);
        }
    }
}
