using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// The header's search: flows by name or description, and steps by name, on the repository's
    /// branch. Every word typed has to be in what it finds, in any order and either case.
    /// </summary>
    public class SearchFlowsHandler
    {
        private const int MAX_FLOWS = 8;
        private const int MAX_STEPS = 12;
        private const int MAX_WORDS = 4;
        private const string ESCAPE = "\\";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public SearchFlowsHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<List<FlowSearchHitDto>>> HandleAsync(string text, CancellationToken ct)
        {
            List<string> patterns = PatternsOf(text);
            if (patterns.Count == 0)
                return ResultDto<List<FlowSearchHitDto>>.Success(new List<FlowSearchHitDto>());

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // A hidden flow is not on the repository's branch, and neither are its steps.
            IQueryable<Flow> flows = dbContext.Flows.AsNoTracking().Where(x => !x.IsHidden);
            IQueryable<FlowStep> steps = dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.FlowStepType != FlowStepTypeEnum.SUCCESS && x.FlowStepType != FlowStepTypeEnum.FAILURE)
                .Where(x => dbContext.Flows.Any(flow => flow.Id == x.RootId && !flow.IsHidden));

            foreach (string pattern in patterns)
            {
                flows = flows.Where(x => EF.Functions.Like(x.Name, pattern, ESCAPE) || EF.Functions.Like(x.Description, pattern, ESCAPE));
                steps = steps.Where(x => EF.Functions.Like(x.Name, pattern, ESCAPE));
            }

            List<FlowSearchHitDto> hits = await flows
                .OrderBy(x => x.Name)
                .Take(MAX_FLOWS)
                .Select(x => new FlowSearchHitDto
                {
                    FlowId = x.Id,
                    FlowName = x.Name,
                    IsSubFlow = x.IsSubFlow,
                    IsQaTest = x.IsQaTest,
                })
                .ToListAsync(ct);

            List<FlowSearchHitDto> stepHits = await steps
                .OrderBy(x => x.Name)
                .Take(MAX_STEPS)
                .Select(x => new FlowSearchHitDto
                {
                    FlowId = x.RootId,
                    FlowName = dbContext.Flows.Where(flow => flow.Id == x.RootId).Select(flow => flow.Name).First(),
                    FlowStepId = x.Id,
                    FlowStepName = x.Name,
                    FlowStepType = x.FlowStepType,
                })
                .ToListAsync(ct);

            hits.AddRange(stepHits);

            return ResultDto<List<FlowSearchHitDto>>.Success(hits);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // One LIKE pattern a word. A % or _ typed is looked for as itself, not as a wildcard.
        private static List<string> PatternsOf(string text)
        {
            return text
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MAX_WORDS)
                .Select(x => $"%{Escaped(x)}%")
                .ToList();
        }

        private static string Escaped(string word)
        {
            return word
                .Replace(ESCAPE, ESCAPE + ESCAPE, StringComparison.Ordinal)
                .Replace("%", ESCAPE + "%", StringComparison.Ordinal)
                .Replace("_", ESCAPE + "_", StringComparison.Ordinal);
        }
    }
}
