using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// What each step of a flow uses, so the Areas, Locations, Inputs and Templates tabs can say
    /// which steps a change to the part they show would reach.
    /// </summary>
    public class GetFlowStepUsesHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowStepUsesHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<FlowStepUseDto>>> HandleAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == flowId)
                .Where(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE)
                .OrderBy(x => x.Id)
                .ToListAsync(ct);

            List<TemplateName> templates = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => x.FlowStep.RootId == flowId)
                .Select(x => new TemplateName(x.FlowStepId, x.FlowTemplate.Name))
                .ToListAsync(ct);

            ILookup<int, string> templatesByStep = templates.ToLookup(x => x.FlowStepId, x => x.Name);

            List<FlowStepUseDto> uses = new List<FlowStepUseDto>();
            foreach (FlowStep step in steps)
            {
                List<string> variableNames = VariableTranslator.VariableBearingText(step)
                    .SelectMany(VariableTranslator.Names)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                uses.Add(new FlowStepUseDto
                {
                    FlowStepId = step.Id,
                    Name = step.Name,
                    FlowStepType = step.FlowStepType,
                    FlowAreaId = step.FlowAreaId,
                    FlowPointId = step.FlowPointId,
                    FlowPointEndId = step.FlowPointEndId,
                    VariableNames = variableNames,
                    TemplateNames = templatesByStep[step.Id].ToList(),
                });
            }

            return ResultDto<List<FlowStepUseDto>>.Success(uses);
        }


        // ================================================================
        // Private records
        // ================================================================

        private sealed record TemplateName(int FlowStepId, string Name);
    }
}
