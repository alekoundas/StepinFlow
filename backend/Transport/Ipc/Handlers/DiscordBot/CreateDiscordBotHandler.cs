using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Transport.Ipc.Handlers
{
    public class CreateDiscordBotHandler
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CreateDiscordBotHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<int>> HandleAsync(DiscordBotDto dto, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            DiscordBot bot = _mapper.Map<DiscordBot>(dto);
            bot.Id = 0;

            dbContext.DiscordBots.Add(bot);
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<int>.Success(bot.Id);
        }
    }
}
