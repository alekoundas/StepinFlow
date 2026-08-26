using Business.Services.NotificationService;
using Core.Models.Business;
using Core.Models.Database;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Services.ExecutionService.Workers
{
    /// <summary>
    /// Always succeeds. A notification that could not be delivered is not a reason to stop execution.
    /// </summary>
    public class NotifyStepWorker : IStepWorker
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IDiscordSendQueue _sendQueue;

        public NotifyStepWorker(IDbContextFactory<AppDbContext> dbContextFactory, IDiscordSendQueue sendQueue)
        {
            _dbContextFactory = dbContextFactory;
            _sendQueue = sendQueue;
        }

        public async Task<ExecutionStep> ExecuteAsync(FlowStep step, IExecutionCacheService cache, CancellationToken ct)
        {
            if (step.DiscordBotId == null)
                return ExecutionStep.Success(message: "No bot selected, so nothing was sent.");

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            DiscordBot? bot = await dbContext.DiscordBots
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == step.DiscordBotId.Value, ct);

            if (bot == null)
                return ExecutionStep.Success(message: "That bot no longer exists.");

            FlowStep? failedStep = null;
            if (step.FlowStepReferenceId != null)
                cache.StepsById.TryGetValue(step.FlowStepReferenceId.Value, out failedStep);

            string flowName = await FlowNameAsync(dbContext, step, ct);
            List<string> templateNames = TemplateNamesOf(failedStep);

            DiscordMessage message = new DiscordMessage
            {
                DiscordBotId = bot.Id,
                WebhookUrl = bot.WebhookUrl,
                BotName = bot.BotName,
                AvatarUrl = bot.AvatarUrl,
                Content = NotifyMessageBuilder.Build(flowName, step, failedStep, templateNames),
            };

            bool queued = _sendQueue.Enqueue(message, TimeSpan.FromSeconds(bot.RateLimitSeconds));

            return ExecutionStep.Success(message: queued ? null : "Dropped: sent again inside the bot's rate limit.");
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<string> FlowNameAsync(AppDbContext dbContext, FlowStep step, CancellationToken ct)
        {
            string? name = await dbContext.Flows
                .AsNoTracking()
                .Where(x => x.Id == step.RootId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

            return name ?? string.Empty;
        }

        private static List<string> TemplateNamesOf(FlowStep? failedStep)
        {
            if (failedStep == null)
                return new List<string>();

            return failedStep.FlowStepImages
                .Select(x => x.Name)
                .ToList();
        }
    }
}
