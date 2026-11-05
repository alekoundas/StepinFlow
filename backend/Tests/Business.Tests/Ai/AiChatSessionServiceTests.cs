using Business.Ai;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging.Abstractions;

namespace Business.Tests.Ai
{
    /// <summary>An answer goes on in the background and says how it went by broadcast. The test plays the model.</summary>
    public sealed class AiChatSessionServiceTests
    {
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();
        private readonly ControlledQuestions _questions = new ControlledQuestions();
        private readonly AiChatSessionService _session;

        public AiChatSessionServiceTests()
        {
            _session = new AiChatSessionService(_questions, _broadcast, NullLogger<AiChatSessionService>.Instance);
        }

        private static AiChatRequestDto Request(string answerId)
        {
            return new AiChatRequestDto { AnswerId = answerId };
        }

        private Task SaidAsync(string answerId, AiChatEventTypeEnum type)
        {
            return _broadcast.SentAsync(x => x is AiChatEventDto chatEvent && chatEvent.AnswerId == answerId && chatEvent.Type == type)
                .WaitAsync(TimeSpan.FromSeconds(5));
        }

        private List<AiChatEventDto> Said()
        {
            lock (_broadcast.Sent)
                return _broadcast.Sent.Where(x => x.Type == BroadcastTypeEnum.AI_CHAT_EVENT).Select(x => (AiChatEventDto)x.Payload!).ToList();
        }

        [Fact]
        public async Task Starting_comes_straight_back_and_the_answer_arrives_by_broadcast()
        {
            _session.Start(Request("a")).ShouldBeTrue();
            Said().ShouldNotContain(x => x.Type == AiChatEventTypeEnum.DONE);

            _questions.Finish("a");

            await SaidAsync("a", AiChatEventTypeEnum.DONE);
        }

        [Fact]
        public void The_same_answer_cannot_start_twice()
        {
            _session.Start(Request("a")).ShouldBeTrue();

            _session.Start(Request("a")).ShouldBeFalse();
        }

        [Fact]
        public async Task Two_conversations_can_be_answering_at_once()
        {
            _session.Start(Request("a")).ShouldBeTrue();
            _session.Start(Request("b")).ShouldBeTrue();

            _questions.Finish("b");

            await SaidAsync("b", AiChatEventTypeEnum.DONE);
            Said().ShouldNotContain(x => x.AnswerId == "a" && x.Type == AiChatEventTypeEnum.DONE);
        }

        [Fact]
        public async Task Stop_ends_that_answer_and_says_so()
        {
            _session.Start(Request("a"));
            _session.Start(Request("b"));

            _session.Stop("a");

            await SaidAsync("a", AiChatEventTypeEnum.STOPPED);
            Said().ShouldNotContain(x => x.AnswerId == "b" && x.Type == AiChatEventTypeEnum.STOPPED);
        }

        [Fact]
        public void Stopping_an_answer_that_has_ended_does_nothing()
        {
            Should.NotThrow(() => _session.Stop("never-started"));
        }

        [Fact]
        public async Task A_model_that_cannot_be_reached_says_why()
        {
            _session.Start(Request("a"));

            _questions.Fail("a", new HttpRequestException("Connection refused."));

            await SaidAsync("a", AiChatEventTypeEnum.FAILED);

            AiChatEventDto failed = Said().Single(x => x.Type == AiChatEventTypeEnum.FAILED);
            failed.Error.ShouldBe("The model could not be reached.");
            failed.ErrorDetail.ShouldBe("Connection refused.");
        }

        // ================================================================
        // Private types
        // ================================================================

        // Each question goes on until the test ends it, or until it is stopped.
        private sealed class ControlledQuestions : IFlowQuestionService
        {
            private readonly Dictionary<string, TaskCompletionSource> _going = new Dictionary<string, TaskCompletionSource>();

            public Task<AiChatAvailabilityDto> GetAvailabilityAsync(CancellationToken ct = default)
            {
                return Task.FromResult(new AiChatAvailabilityDto { IsAvailable = true });
            }

            public async Task AskAsync(AiChatRequestDto request, Action<AiChatEventDto> report, CancellationToken ct = default)
            {
                await Going(request.AnswerId).Task.WaitAsync(ct);

                report(new AiChatEventDto { AnswerId = request.AnswerId, Type = AiChatEventTypeEnum.DONE });
            }

            public void Finish(string answerId)
            {
                Going(answerId).TrySetResult();
            }

            public void Fail(string answerId, Exception failure)
            {
                Going(answerId).TrySetException(failure);
            }

            private TaskCompletionSource Going(string answerId)
            {
                lock (_going)
                {
                    if (!_going.TryGetValue(answerId, out TaskCompletionSource? going))
                    {
                        going = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                        _going[answerId] = going;
                    }

                    return going;
                }
            }
        }
    }
}
