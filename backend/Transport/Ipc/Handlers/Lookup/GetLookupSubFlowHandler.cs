using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Every flow that may be invoked, which is every flow flagged as a sub-flow.
    ///
    /// Deliberately unfiltered beyond that. A flow may invoke one that invokes it back, and a
    /// sub-flow may invoke itself: the app already lets a Loop run forever on purpose, so
    /// refusing a cycle here would be the one rule out of step with that, and a self call with
    /// an exit condition is recursion rather than a mistake.
    /// </summary>
    public class GetLookupSubFlowHandler
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupSubFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            IQueryable<Core.Models.Database.Flow> query = dbContext.Flows
                .AsNoTracking()
                .Where(x => x.IsSubFlow);

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                query = query.Where(x => x.Name.Contains(dto.SearchText));

            List<LookupItemDto> items = await query
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(CultureInfo.InvariantCulture),
                    Label = x.Name,
                    Description = $"{x.FlowSteps.Count(step => step.ParentFlowStepId == null)} step(s)",
                })
                .ToListAsync(ct);

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto
            {
                Data = items,
                TotalRecords = items.Count,
            });
        }
    }
}
