using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Every template image a flow has, with its picture and how many steps link to it.
    /// </summary>
    public class GetFlowTemplatesHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowTemplatesHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<FlowTemplateDto>>> HandleAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowTemplateDto> templates = await dbContext.FlowTemplates
                .AsNoTracking()
                .Where(x => x.FlowId == flowId)
                .OrderBy(x => x.Name)
                .Select(x => new FlowTemplateDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    TemplateImage = x.TemplateImage,
                    ClickOffsetX = x.ClickOffsetX,
                    ClickOffsetY = x.ClickOffsetY,
                    AuthoredFlowAreaWidth = x.AuthoredFlowAreaWidth,
                    AuthoredFlowAreaHeight = x.AuthoredFlowAreaHeight,
                    AuthoredDpi = x.AuthoredDpi,
                    FlowId = x.FlowId,
                    FlowStepsCount = x.FlowStepTemplates.Count(),
                })
                .ToListAsync(ct);

            return ResultDto<List<FlowTemplateDto>>.Success(templates);
        }
    }
}
