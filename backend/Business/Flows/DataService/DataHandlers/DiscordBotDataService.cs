using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Core.Models.Database;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.DataService.DataHandlers
{
    /// <summary>
    /// Between a script's Notify steps and the Discord bots of this machine, which a script knows
    /// only by name: the name to the bot on an import, the bot to its name on an export.
    /// </summary>
    public sealed class DiscordBotDataService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DiscordBotDataService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Points each Notify step of a script at the Discord bot it names, the same name exactly
        /// first, else any case. A name no bot here has is added as a bot with no webhook, saved with
        /// the steps, so they keep it and the user only pastes the webhook in; it comes back as one
        /// warning, at its line under Discord Bots.
        /// </summary>
        public async Task<List<Diagnostic>> LinkAsync(FlowScriptSchema schema, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            return await LinkAsync(dbContext, schema, ct);
        }

        /// <summary>The same, inside the caller's transaction.</summary>
        public static async Task<List<Diagnostic>> LinkAsync(AppDbContext dbContext, FlowScriptSchema schema, CancellationToken ct)
        {
            List<Diagnostic> warnings = new List<Diagnostic>();
            if (schema.DiscordBots.Count == 0)
                return warnings;

            List<DiscordBot> bots = await dbContext.DiscordBots
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new DiscordBot { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            // One new bot per name, for every step that sends through it.
            Dictionary<string, DiscordBot> added = new Dictionary<string, DiscordBot>(StringComparer.OrdinalIgnoreCase);

            foreach ((FlowStep step, DiscordBotReference discordBot) in schema.DiscordBots)
            {
                DiscordBot? target = bots.FirstOrDefault(x => string.Equals(x.Name, discordBot.Name, StringComparison.Ordinal))
                    ?? bots.FirstOrDefault(x => string.Equals(x.Name, discordBot.Name, StringComparison.OrdinalIgnoreCase));

                if (target != null)
                {
                    step.DiscordBotId = target.Id;
                    continue;
                }

                if (!added.TryGetValue(discordBot.Name, out DiscordBot? webhookless))
                {
                    webhookless = new DiscordBot { Name = discordBot.Name };
                    added[discordBot.Name] = webhookless;

                    warnings.Add(Diagnostic.Warning(DiagnosticCodeEnum.DISCORD_BOT_UNKNOWN, discordBot.Line, 1,
                        $"No Discord bot here is called \"{discordBot.Name}\", so one is added by that name with no webhook. What it sends goes nowhere until its webhook is pasted in under Settings, Discord bots."));
                }

                step.DiscordBot = webhookless;
            }

            return warnings;
        }

        /// <summary>
        /// Names the bot of each Notify the schema has one for and does not name yet: a flow loaded
        /// for export, or a draft whose new steps have only the bot picked in the review.
        /// </summary>
        public async Task NameAsync(FlowScriptSchema schema, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            await NameAsync(dbContext, schema, ct);
        }

        /// <summary>The same, inside the caller's context.</summary>
        public static async Task NameAsync(AppDbContext dbContext, FlowScriptSchema schema, CancellationToken ct)
        {
            List<int> botIds = schema.Steps
                .Where(x => x.DiscordBotId != null)
                .Select(x => x.DiscordBotId!.Value)
                .Distinct()
                .ToList();

            if (botIds.Count == 0)
                return;

            Dictionary<int, string> names = await dbContext.DiscordBots.AsNoTracking()
                .Where(x => botIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            foreach (FlowStep step in schema.Steps.Where(x => x.DiscordBotId != null && !schema.DiscordBots.ContainsKey(x)))
            {
                if (names.TryGetValue(step.DiscordBotId!.Value, out string? name))
                    schema.DiscordBots[step] = new DiscordBotReference { Name = name };
            }
        }
    }
}
