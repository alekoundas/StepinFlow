using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyDiscordBotHandler : IRequestHandler<GetLazyDiscordBotQuery, ResultDto<LazyResponseDto<DiscordBotDto>>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyDiscordBotHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<DiscordBotDto>>> Handle(GetLazyDiscordBotQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // Projected so the usage count is a subquery rather than a loaded collection.
            List<DiscordBotDto> dtos = await dbContext.DiscordBots
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new DiscordBotDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    WebhookUrl = x.WebhookUrl,
                    BotName = x.BotName,
                    AvatarUrl = x.AvatarUrl,
                    RateLimitSeconds = x.RateLimitSeconds,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn,
                    FlowStepsCount = x.FlowSteps.Count(),
                })
                .ToListAsync(ct);

            return ResultDto<LazyResponseDto<DiscordBotDto>>.Success(new LazyResponseDto<DiscordBotDto>
            {
                Data = dtos,
                TotalRecords = dtos.Count,
            });
        }
    }
}
