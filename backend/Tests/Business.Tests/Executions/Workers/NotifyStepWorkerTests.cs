using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;

using DataAccess;

namespace Business.Tests.Executions.Workers
{
    /// <summary>
    /// The one worker that reads the database itself, so it is tested against a real one. Twelve
    /// workers talk to ports and this one talks to a DbContext, which is worth knowing.
    /// </summary>
    public sealed class NotifyStepWorkerTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly FakeDiscordSendQueue _queue = new FakeDiscordSendQueue();

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private (int FlowId, int BotId) Seed()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login smoke", PublicId = Guid.NewGuid() };
            DiscordBot bot = new DiscordBot { Name = "Team", BotName = "StepinFlow", WebhookUrl = "https://discord.example/hook", RateLimitSeconds = 30 };
            db.Flows.Add(flow);
            db.DiscordBots.Add(bot);
            db.SaveChanges();

            return (flow.Id, bot.Id);
        }

        private async Task<ExecutionStep> Notify(FlowStep notify, params FlowStep[] others)
        {
            ExecutionCacheService cache = await WorkerCache.ForAsync([notify, .. others]);
            return await new NotifyStepWorker(_database, _queue).ExecuteAsync(notify, cache, Ct);
        }

        [Fact]
        public async Task A_message_goes_through_the_bots_webhook_at_the_bots_rate()
        {
            (int flowId, int botId) = Seed();

            await Notify(new FlowStep { Id = 1, RootId = flowId, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = botId, Message = "checkout failed" });

            (Core.Models.Business.DiscordMessage message, TimeSpan interval) = _queue.Queued.Single();
            message.WebhookUrl.ShouldBe("https://discord.example/hook");
            message.BotName.ShouldBe("StepinFlow");
            message.Content.ShouldContain("Login smoke");
            message.Content.ShouldContain("checkout failed");
            interval.ShouldBe(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task Reporting_a_failed_search_lists_its_templates_with_their_accuracy()
        {
            (int flowId, int botId) = Seed();
            FlowStep failed = new FlowStep { Id = 2, Name = "Find login", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, FlowStepTemplates = [new FlowStepTemplate { Name = "login button", Accuracy = 0.85f }] };

            await Notify(new FlowStep { Id = 1, RootId = flowId, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = botId, FlowStepReferenceId = 2 }, failed);

            _queue.Queued.Single().Message.Content.ShouldContain("login button at 0.85");
        }

        [Fact]
        public async Task No_bot_sends_nothing_and_passes()
        {
            ExecutionStep result = await Notify(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.NOTIFY });

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            _queue.Queued.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_deleted_bot_sends_nothing_and_passes()
        {
            ExecutionStep result = await Notify(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = 999 });

            result.Message.ShouldBe("That bot no longer exists.");
            _queue.Queued.ShouldBeEmpty();
        }

        // A notification is never the reason an execution fails.
        [Fact]
        public async Task A_message_dropped_by_the_rate_limit_still_passes_and_says_so()
        {
            (int flowId, int botId) = Seed();
            _queue.Accepts = false;

            ExecutionStep result = await Notify(new FlowStep { Id = 1, RootId = flowId, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = botId });

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Message.ShouldBe("Dropped: sent again inside the bot's rate limit.");
        }
    }
}
