using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowSearchAreaHandler : IRequestHandler<UpdateFlowSearchAreaCommand, ResultDto<FlowSearchAreaDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowSearchAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowSearchAreaDto>> Handle(UpdateFlowSearchAreaCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowSearchArea? existingFlowSearchArea = await dbContext.FlowSearchAreas
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlowSearchArea == null)
                return ResultDto<FlowSearchAreaDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingFlowSearchArea).CurrentValues.SetValues(request.dto);

            await dbContext.SaveChangesAsync(ct);

            FlowSearchAreaDto flowSearchAreaDto = _mapper.Map<FlowSearchAreaDto>(existingFlowSearchArea);
            return ResultDto<FlowSearchAreaDto>.Success(flowSearchAreaDto);
        }
    }
}
