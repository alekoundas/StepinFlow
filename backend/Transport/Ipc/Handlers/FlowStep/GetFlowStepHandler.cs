using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class GetFlowStepHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowStepDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            FlowStep? flowStep = await dbContext.FlowSteps
                .AsNoTracking()
                .Include(x => x.FlowStepTemplates.OrderBy(image => image.OrderNumber))
                .ThenInclude(x => x.FlowTemplate)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (flowStep == null)
                return ResultDto<FlowStepDto>.Failure("Entity doesnt exist in the Database!");

            FlowStepDto? flowStepDto = _mapper.Map<FlowStepDto>(flowStep);

            // Counted over the whole flow, so the form can say a picture is shared before it is edited.
            List<int> pictureIds = flowStep.FlowStepTemplates.Select(x => x.FlowTemplateId).ToList();
            Dictionary<int, int> stepCounts = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => pictureIds.Contains(x.FlowTemplateId))
                .GroupBy(x => x.FlowTemplateId)
                .Select(x => new { FlowTemplateId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.FlowTemplateId, x => x.Count, ct);

            foreach (FlowStepTemplateDto template in flowStepDto.FlowStepTemplates.Where(x => x.FlowTemplate != null))
                template.FlowTemplate!.FlowStepsCount = stepCounts.GetValueOrDefault(template.FlowTemplateId);

            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
