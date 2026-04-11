using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowHandler : IRequestHandler<UpdateFlowCommand, ResultDto<FlowDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dataService = dataService;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowDto>> Handle(UpdateFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
            Flow existingFlow = await dbContext.Flows.Include(x => x.FlowSearchAreas).FirstAsync(x => x.Id == request.dto.Id);

            if (existingFlow == null)
                return ResultDto<FlowDto>.Failure("Flow not found");

            _mapper.Map(request.dto, existingFlow);   

            //int count = await _dataService.UpdateAsync(existingFlow);
            int count = await dbContext.SaveChangesAsync(ct);
            if (count <= 0)
                return ResultDto<FlowDto>.Failure("No changes made to the Database!");

            var updatedDto = _mapper.Map<FlowDto>(existingFlow);
            return ResultDto<FlowDto>.Success(updatedDto);
        }
    }
}
