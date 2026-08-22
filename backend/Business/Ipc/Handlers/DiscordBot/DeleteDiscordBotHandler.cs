using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Refused while any step still points at it.
    ///
    /// This is the one place the app does not let the user build something broken. A dangling
    /// reference inside a flow is visible in the flow the user is editing; a deleted bot breaks
    /// steps in flows they are not looking at, so it is refused rather than flagged afterwards.
    /// The refusal names the flows, otherwise the user is left hunting for them.
    /// </summary>
    public class DeleteDiscordBotHandler : IRequestHandler<DeleteDiscordBotCommand, ResultDto<bool>>
    {
        private const int NamesShown = 5;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DeleteDiscordBotHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(DeleteDiscordBotCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            List<string> users = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.DiscordBotId == request.id)
                .Select(x => x.Flow.Name + " - " + x.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

            if (users.Count > 0)
                return ResultDto<bool>.Failure(Refusal(users));

            int count = await dbContext.DiscordBots
                .Where(x => x.Id == request.id)
                .ExecuteDeleteAsync(ct);

            if (count <= 0)
                return ResultDto<bool>.Failure("That Discord bot no longer exists.");

            return ResultDto<bool>.Success(true);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Refusal(List<string> users)
        {
            string listed = string.Join(", ", users.Take(NamesShown));

            string rest = users.Count > NamesShown
                ? $" and {users.Count - NamesShown} more"
                : string.Empty;

            return $"{users.Count} step(s) still send through this bot: {listed}{rest}. " +
                   "Point them at another bot, or remove them, then delete it.";
        }
    }
}
