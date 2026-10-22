using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Every template a flow searches for, one per name. Steps that search for the same image each
    /// keep their own copy of it under the same name, which is also how the export writes it once.
    /// </summary>
    public class GetFlowTemplatesHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowTemplatesHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<FlowStepTemplateDto>>> HandleAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowStepTemplate> templates = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.FlowStepId)
                .ToListAsync(ct);

            List<FlowStepTemplateDto> distinct = templates
                .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => _mapper.Map<FlowStepTemplateDto>(x))
                .ToList();

            return ResultDto<List<FlowStepTemplateDto>>.Success(distinct);
        }
    }
}
