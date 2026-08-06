using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepImageHandler : IRequestHandler<CreateFlowStepImageCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateFlowStepImageHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowStepImageCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStepImage flowStepImage = _mapper.Map<FlowStepImage>(request.dto);
            flowStepImage.Id = 0;

            dbContext.FlowStepImages.Add(flowStepImage);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(flowStepImage.Id);
        }
    }
}
