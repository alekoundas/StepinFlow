using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateSubFlowHandler : IRequestHandler<UpdateSubFlowCommand, ResultDto<SubFlowDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateSubFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<SubFlowDto>> Handle(UpdateSubFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            SubFlow? existingSubFlow = await dbContext.SubFlows
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingSubFlow == null)
                return ResultDto<SubFlowDto>.Failure("Entity doesnt exist in the Database!");

            dbContext.Entry(existingSubFlow).CurrentValues.SetValues(request.dto);

            await dbContext.SaveChangesAsync(ct);

            SubFlowDto subFlowDto = _mapper.Map<SubFlowDto>(existingSubFlow);
            return ResultDto<SubFlowDto>.Success(subFlowDto);
        }
    }
}
