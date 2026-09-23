using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class UpdateDiscordBotHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateDiscordBotHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<DiscordBotDto>> HandleAsync(DiscordBotDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            DiscordBot? existing = await dbContext.DiscordBots
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (existing == null)
                return ResultDto<DiscordBotDto>.Failure("That Discord bot no longer exists.");

            // FlowStepsCount is projected for the list and has no column, so it is not settable here.
            existing.Name = dto.Name;
            existing.WebhookUrl = dto.WebhookUrl;
            existing.BotName = dto.BotName;
            existing.AvatarUrl = dto.AvatarUrl;
            existing.RateLimitSeconds = dto.RateLimitSeconds;

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<DiscordBotDto>.Success(_mapper.Map<DiscordBotDto>(existing));
        }
    }
}
