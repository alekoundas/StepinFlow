using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowLocationHandler : IRequestHandler<UpdateFlowLocationCommand, ResultDto<FlowLocationDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowLocationHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowLocationDto>> Handle(UpdateFlowLocationCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowLocation? existingFlowLocation = await dbContext.FlowLocations
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlowLocation == null)
                return ResultDto<FlowLocationDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingFlowLocation).CurrentValues.SetValues(request.dto);

            await dbContext.SaveChangesAsync(ct);

            FlowLocationDto flowLocationDto = _mapper.Map<FlowLocationDto>(existingFlowLocation);
            return ResultDto<FlowLocationDto>.Success(flowLocationDto);
        }
    }
}
