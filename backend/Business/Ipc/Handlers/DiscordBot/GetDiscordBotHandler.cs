using AutoMapper;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetDiscordBotHandler : IRequestHandler<GetDiscordBotQuery, ResultDto<DiscordBotDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetDiscordBotHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<DiscordBotDto>> Handle(GetDiscordBotQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Core.Models.Database.DiscordBot? bot = await dbContext.DiscordBots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.id, ct);

            if (bot == null)
                return ResultDto<DiscordBotDto>.Failure("That Discord bot no longer exists.");

            return ResultDto<DiscordBotDto>.Success(_mapper.Map<DiscordBotDto>(bot));
        }
    }
}
