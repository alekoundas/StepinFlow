using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
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
                .FirstOrDefaultAsync(x => x.Id == request.Dto.Id, ct);

            if (existing == null)
                return ResultDto<DiscordBotDto>.Failure("That Discord bot no longer exists.");

            // FlowStepsCount is projected for the list and has no column, so it is not settable here.
            existing.Name = request.Dto.Name;
            existing.WebhookUrl = request.Dto.WebhookUrl;
            existing.BotName = request.Dto.BotName;
            existing.AvatarUrl = request.Dto.AvatarUrl;
            existing.RateLimitSeconds = request.Dto.RateLimitSeconds;

            await dbContext.SaveChangesAsync(ct);

            return ResultDto<DiscordBotDto>.Success(_mapper.Map<DiscordBotDto>(existing));
        }
    }
}
