using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateDiscordBotHandler : IRequestHandler<UpdateDiscordBotCommand, ResultDto<DiscordBotDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateDiscordBotHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<DiscordBotDto>> Handle(UpdateDiscordBotCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            DiscordBot? existing = await dbContext.DiscordBots
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existing == null)
                return ResultDto<DiscordBotDto>.Failure("That Discord bot no longer exists.");

            // FlowStepsCount is projected for the list and has no column, so it is not settable here.
            existing.Name = request.dto.Name;
            existing.WebhookUrl = request.dto.WebhookUrl;
            existing.BotName = request.dto.BotName;
            existing.AvatarUrl = request.dto.AvatarUrl;
            existing.RateLimitSeconds = request.dto.RateLimitSeconds;

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<DiscordBotDto>.Success(_mapper.Map<DiscordBotDto>(existing));
        }
    }
}
