using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowAreaHandler : IRequestHandler<UpdateFlowAreaCommand, ResultDto<FlowAreaDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowAreaHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowAreaDto>> Handle(UpdateFlowAreaCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea? existingFlowArea = await dbContext.FlowAreas
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlowArea == null)
                return ResultDto<FlowAreaDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingFlowArea).CurrentValues.SetValues(request.dto);

            await dbContext.SaveChangesAsync(ct);

            FlowAreaDto flowAreaDto = _mapper.Map<FlowAreaDto>(existingFlowArea);
            return ResultDto<FlowAreaDto>.Success(flowAreaDto);
        }
    }
}
