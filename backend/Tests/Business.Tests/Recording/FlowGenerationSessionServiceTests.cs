using Business.Recording.Generation;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// One generation or revision at a time, in the background, said by broadcast. The generation
    /// itself is played by the test, which decides when it ends.
    /// </summary>
    public sealed class FlowGenerationSessionServiceTests : IDisposable
    {
        private readonly FakeIpcBroadcastService _broadcast = new FakeIpcBroadcastService();
        private readonly ControlledGeneration _generation = new ControlledGeneration();
        private readonly FlowGenerationSessionService _session;

        public FlowGenerationSessionServiceTests()
        {
            _session = new FlowGenerationSessionService(_generation, _broadcast);
        }

        public void Dispose()
        {
            _session.Dispose();
        }

        private Task<ResultDto<bool>> StartAsync()
        {
            return _session.StartAsync(1, new RecordingResultDto(), new ReviewedRecordingDto(), x => null);
        }

        // Null until the generation's first stage says something.
        private FlowGenerationProgressDto? LastProgress()
        {
            lock (_broadcast.Sent)
                return _broadcast.Sent.Where(x => x.Type == BroadcastTypeEnum.FLOW_GENERATION_PROGRESS).Select(x => (FlowGenerationProgressDto)x.Payload!).LastOrDefault();
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));

            while (!condition())
                await Task.Delay(10, timeout.Token);
        }

        [Fact]
        public async Task A_generation_says_DONE_and_its_result_is_there()
        {
            (await StartAsync()).IsSuccess.ShouldBeTrue();
            _session.GetResult().ShouldBeNull();

            FlowGenerationResultDto result = new FlowGenerationResultDto { IsModelDraft = true };
            _generation.Finish(result);
            await WaitUntilAsync(() => LastProgress()?.State == FlowGenerationStateEnum.DONE);

            _session.GetResult().ShouldBeSameAs(result);
        }

        [Fact]
        public async Task A_second_start_while_one_is_going_is_refused()
        {
            await StartAsync();

            ResultDto<bool> second = await StartAsync();

            second.ErrorMessage.ShouldBe("The flow is already being generated.");
        }

        [Fact]
        public async Task A_cancel_saves_nothing_and_says_NONE()
        {
            await StartAsync();

            await _session.CancelAsync();

            LastProgress().ShouldNotBeNull().State.ShouldBe(FlowGenerationStateEnum.NONE);
            _session.GetResult().ShouldBeNull();
            _generation.WasCancelled.ShouldBeTrue();
        }

        [Fact]
        public async Task A_generation_that_throws_says_DONE_with_why_and_has_no_result()
        {
            await StartAsync();

            _generation.Fail(new InvalidOperationException("There is no flow 1 to export."));
            await WaitUntilAsync(() => LastProgress()?.State == FlowGenerationStateEnum.DONE);

            LastProgress().ShouldNotBeNull().ErrorMessage.ShouldBe("Nothing was saved. There is no flow 1 to export.");
            _session.GetResult().ShouldBeNull();
        }

        // The Check page's "Ask for changes" goes the same way, one at a time with a generation.
        [Fact]
        public async Task A_revision_says_DONE_and_its_result_is_there()
        {
            RecordingDraftRevisionDto revision = new RecordingDraftRevisionDto { FlowId = 1, Changes = "Sign out at the end." };
            (await _session.StartRevisionAsync(revision, [])).IsSuccess.ShouldBeTrue();
            (await StartAsync()).ErrorMessage.ShouldBe("The flow is already being generated.");

            FlowGenerationResultDto result = new FlowGenerationResultDto { IsModelDraft = true };
            _generation.Finish(result);
            await WaitUntilAsync(() => LastProgress()?.State == FlowGenerationStateEnum.DONE);

            _generation.Changes.ShouldBe("Sign out at the end.");
            _session.GetResult().ShouldBeSameAs(result);
        }


        // ================================================================
        // Private types
        // ================================================================

        // Ends when the test says, or when cancelled.
        private sealed class ControlledGeneration : IFlowGenerationService
        {
            private readonly TaskCompletionSource<FlowGenerationResultDto> _done = new TaskCompletionSource<FlowGenerationResultDto>(TaskCreationOptions.RunContinuationsAsynchronously);

            public bool WasCancelled { get; private set; }

            public void Finish(FlowGenerationResultDto result)
            {
                _done.TrySetResult(result);
            }

            public void Fail(Exception ex)
            {
                _done.TrySetException(ex);
            }

            public string? Changes { get; private set; }

            public Task<FlowGenerationResultDto> ImportModelDraftAsync(
                int flowId,
                RecordingResultDto recording,
                ReviewedRecordingDto reviewed,
                Func<int, RecordingScreenDto?> screenAt,
                Action<FlowGenerationProgressDto>? onProgress,
                CancellationToken ct = default)
            {
                return WaitAsync(ct);
            }

            public Task<FlowGenerationResultDto> ReviseAsync(int flowId, string changes, IReadOnlyCollection<string> undecided, IReadOnlyList<string> screenLines, Action<FlowGenerationProgressDto>? onProgress, CancellationToken ct = default)
            {
                Changes = changes;
                return WaitAsync(ct);
            }

            public Task<ResultDto<FlowGenerationResultDto>> DropCheckAsync(ProposedCheckDropDto drop, IReadOnlyList<string> screenLines, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            private async Task<FlowGenerationResultDto> WaitAsync(CancellationToken ct)
            {
                using CancellationTokenRegistration registration = ct.Register(() =>
                {
                    WasCancelled = true;
                    _done.TrySetCanceled(ct);
                });

                return await _done.Task;
            }

            public Task<ResultDto<FlowGenerationResultDto>> ImportPlainDraftAsync(int flowId, RecordingResultDto recording, ReviewedRecordingDto reviewed, Func<int, RecordingScreenDto?> screenAt, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }

            public Task<ResultDto<FlowScriptImportResultDto>> InsertPlainDraftAsync(RecordingDraftPositionDto position, RecordingResultDto recording, ReviewedRecordingDto reviewed, Func<int, RecordingScreenDto?> screenAt, CancellationToken ct = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
