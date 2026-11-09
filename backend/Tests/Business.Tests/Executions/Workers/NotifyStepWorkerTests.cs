using Business.Executions;
using Business.Executions.Workers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;

using DataAccess;
using static Business.Tests.TestToken;

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

        // What the step above came back with, recorded the way the engine records it before the walk goes under it.
        private async Task<string> NotifyAfter(FlowStep reported, ExecutionStep reportedResult, string message = "")
        {
            (int flowId, int botId) = Seed();
            FlowStep notify = new FlowStep { Id = 1, RootId = flowId, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = botId, FlowStepReferenceId = reported.Id, Message = message };

            ExecutionCacheService cache = await WorkerCache.ForAsync([notify, reported]);
            cache.RecordExecutionStep(reported.Id, reportedResult);
            await new NotifyStepWorker(_database, _queue).ExecuteAsync(notify, cache, Ct);

            return _queue.Queued.Single().Message.Content;
        }

        [Fact]
        public async Task A_failed_image_search_says_how_close_it_came_against_what_was_needed()
        {
            FlowStep search = new FlowStep { Id = 2, Name = "Find login", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, FlowStepTemplates = [new FlowStepTemplate { Id = 5, Accuracy = 0.85f, FlowTemplate = new FlowTemplate { Name = "login button" } }] };
            ExecutionStep missed = ExecutionStep.Failure("no template matched");
            missed.BestScore = 0.71f;
            missed.BestTemplateId = 5;

            string content = await NotifyAfter(search, missed);

            content.ShouldContain("Find login — Search Image");
            content.ShouldContain("Not found. Closest was \"login button\" at 0.71 accuracy, 0.85 needed.");
        }

        [Fact]
        public async Task A_found_image_search_says_what_it_found_at_what_accuracy()
        {
            FlowStep search = new FlowStep { Id = 2, Name = "Find login", FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, FlowStepTemplates = [new FlowStepTemplate { Id = 5, Accuracy = 0.85f, FlowTemplate = new FlowTemplate { Name = "login button" } }] };
            ExecutionStep hit = ExecutionStep.Success();
            hit.BestScore = 0.94f;
            hit.BestTemplateId = 5;

            string content = await NotifyAfter(search, hit);

            content.ShouldContain("Found \"login button\" at 0.94 accuracy, 0.85 needed.");
        }

        [Fact]
        public async Task A_text_search_gives_the_text_it_read_above_the_users_message()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "Read order", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, SearchMode = SearchModeEnum.FIND_BEST };
            ExecutionStep found = ExecutionStep.Success();
            found.Value = "Order 1234";

            string content = await NotifyAfter(read, found, "checkout finished");

            content.ShouldContain("Text found: \"Order 1234\"");
            content.IndexOf("Order 1234", StringComparison.Ordinal).ShouldBeLessThan(content.IndexOf("checkout finished", StringComparison.Ordinal));
        }

        [Fact]
        public async Task A_step_that_could_not_get_going_says_why_in_the_engines_words()
        {
            FlowStep read = new FlowStep { Id = 2, Name = "Read price", FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, SearchMode = SearchModeEnum.FIND_BEST };

            string content = await NotifyAfter(read, ExecutionStep.Failure("No chrome window has \"shop.example.com\" in front."));

            content.ShouldContain("Not found: No chrome window has \"shop.example.com\" in front.");
        }

        [Fact]
        public async Task A_command_gives_its_exit_code_and_what_it_printed()
        {
            FlowStep command = new FlowStep { Id = 2, Name = "Disk space", FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND };
            ExecutionStep ran = ExecutionStep.Success();
            ran.ExitCode = 0;
            ran.Value = "C: 41 GB free";

            string content = await NotifyAfter(command, ran);

            content.ShouldContain("Exit code 0.\n```\nC: 41 GB free\n```");
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

        // An import adds a bot its file named with no webhook, for the user to paste one in.
        [Fact]
        public async Task A_bot_with_no_webhook_yet_sends_nothing_and_passes_saying_so()
        {
            int botId;
            using (AppDbContext db = _database.CreateDbContext())
            {
                DiscordBot bot = new DiscordBot { Name = "Alerts" };
                db.DiscordBots.Add(bot);
                db.SaveChanges();
                botId = bot.Id;
            }

            ExecutionStep result = await Notify(new FlowStep { Id = 1, FlowStepType = FlowStepTypeEnum.NOTIFY, DiscordBotId = botId });

            result.Outcome.ShouldBe(StepOutcomeEnum.SUCCESS);
            result.Message!.ShouldStartWith("\"Alerts\" has no webhook yet, so nothing was sent.");
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
