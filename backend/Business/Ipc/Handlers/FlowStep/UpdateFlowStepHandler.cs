using AutoMapper;
using Business.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowStepHandler : IRequestHandler<UpdateFlowStepCommand, ResultDto<FlowStepDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepDto>> Handle(UpdateFlowStepCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep? existingFlowStep = await dbContext.FlowSteps
                .Include(x => x.FlowStepImages)
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlowStep == null)
                return ResultDto<FlowStepDto>.Failure("Entity doesnt exist in the Database!");

            // SetValues copies scalars and foreign keys only, so the navigations the client
            // round-tripped back to us cannot re-insert or overwrite anything, and CreatedOn
            // (absent from the dto) keeps its original value.
            dbContext.Entry(existingFlowStep).CurrentValues.SetValues(request.dto);

            FlowStepImageSyncHelper.Sync(dbContext, existingFlowStep, request.dto.FlowStepImages);

            await dbContext.SaveChangesAsync(ct);

            FlowStepDto flowStepDto = _mapper.Map<FlowStepDto>(existingFlowStep);
            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
