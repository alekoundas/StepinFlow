using Business.Services.FlowValidationService;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// One answer per flow rather than one per branch: checking whether a step can read another
    /// step's result needs the whole parent chain, so a per branch check would reload the same
    /// tree on every expand.
    /// </summary>
    public class ValidateFlowHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFlowValidationService _flowValidationService;

        public ValidateFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory, IFlowValidationService flowValidationService)
        {
            _dbContextFactory = dbContextFactory;
            _flowValidationService = flowValidationService;
        }

        public async Task<ResultDto<FlowValidationResultDto>> HandleAsync(int id, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == id)
                .ToListAsync(ct);

            // Counted rather than Included: the templates themselves are megabytes and only their
            // number matters here.
            Dictionary<int, int> templateCounts = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => x.FlowStep.RootId == id)
                .GroupBy(x => x.FlowStepId)
                .Select(x => new { FlowStepId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.FlowStepId, x => x.Count, ct);

            // Only what the rules read: the name, and what each one sits in.
            List<FlowArea> areas = await dbContext.FlowAreas
                .AsNoTracking()
                .Where(x => x.FlowId == id)
                .Select(x => new FlowArea { Id = x.Id, Name = x.Name, Type = x.Type, ParentFlowAreaId = x.ParentFlowAreaId })
                .ToListAsync(ct);

            List<FlowPoint> points = await dbContext.FlowPoints
                .AsNoTracking()
                .Where(x => x.FlowId == id)
                .Select(x => new FlowPoint { Id = x.Id, Name = x.Name, FlowAreaId = x.FlowAreaId })
                .ToListAsync(ct);

            List<string> inputNames = await dbContext.FlowCsvColumns
                .AsNoTracking()
                .Where(x => x.FlowId == id)
                .Select(x => x.Name)
                .ToListAsync(ct);

            // Areas, points and csv columns share a namespace with steps, and a {{name}} resolves
            // against any of them, so neither question is answerable without all four.
            List<string> flowNames = areas.Select(x => x.Name)
                .Concat(points.Select(x => x.Name))
                .Concat(inputNames)
                .ToList();

            FlowValidationResultDto result = _flowValidationService.Validate(steps, templateCounts, areas, points, flowNames);

            return ResultDto<FlowValidationResultDto>.Success(result);
        }
    }
}
