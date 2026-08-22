using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// The bots a Notify step can send through. The webhook URL is deliberately not returned:
    /// a dropdown never needs the credential, and this response reaches the renderer.
    /// </summary>
    public class GetLookupDiscordBotHandler : IRequestHandler<GetLookupDiscordBotQuery, ResultDto<LookupResponseDto>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupDiscordBotHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupDiscordBotQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            IQueryable<Core.Models.Database.DiscordBot> query = dbContext.DiscordBots.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(dto.SearchText))
                query = query.Where(x => x.Name.Contains(dto.SearchText));

            List<LookupItemDto> items = await query
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,

                    // The interval belongs in the step form, so the throttle is visible where the
                    // step is built rather than discovered when messages go missing.
                    Description = $"one message every {x.RateLimitSeconds}s at most",
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
