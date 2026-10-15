using Business.Recording.Generation;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using static Business.Tests.TestToken;

namespace Business.Tests.Recording
{
    /// <summary>
    /// One generation at a time, in the background, said by broadcast. The generation itself is
    /// played by the test, which decides when it ends.
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

        private FlowGenerationProgressDto LastProgress()
        {
            lock (_broadcast.Sent)
                return (FlowGenerationProgressDto)_broadcast.Sent.Last(x => x.Type == BroadcastTypeEnum.FLOW_GENERATION_PROGRESS).Payload!;
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
            LastProgress().State.ShouldBe(FlowGenerationStateEnum.GENERATING);
            _session.GetResult().ShouldBeNull();

            FlowGenerationResultDto result = new FlowGenerationResultDto { IsModelDraft = true };
            _generation.Finish(result);
            await WaitUntilAsync(() => LastProgress().State == FlowGenerationStateEnum.DONE);

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

            LastProgress().State.ShouldBe(FlowGenerationStateEnum.NONE);
            _session.GetResult().ShouldBeNull();
            _generation.WasCancelled.ShouldBeTrue();
        }

        [Fact]
        public async Task A_generation_that_throws_says_DONE_with_why_and_has_no_result()
        {
            await StartAsync();

            _generation.Fail(new InvalidOperationException("There is no flow 1 to export."));
            await WaitUntilAsync(() => LastProgress().State == FlowGenerationStateEnum.DONE);

            LastProgress().ErrorMessage.ShouldBe("Nothing was saved. There is no flow 1 to export.");
            _session.GetResult().ShouldBeNull();
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

            public async Task<FlowGenerationResultDto> ImportModelDraftAsync(
                int flowId,
                RecordingResultDto recording,
                ReviewedRecordingDto reviewed,
                Func<int, RecordingScreenDto?> screenAt,
                Action<FlowGenerationProgressDto>? onProgress,
                CancellationToken ct = default)
            {
                using CancellationTokenRegistration registration = ct.Register(() =>
                {
                    WasCancelled = true;
                    _done.TrySetCanceled(ct);
                });

                return await _done.Task;
            }

            public Task<FlowScriptImportResultDto> ImportPlainDraftAsync(int flowId, RecordingResultDto recording, ReviewedRecordingDto reviewed, Func<int, RecordingScreenDto?> screenAt, CancellationToken ct = default)
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
