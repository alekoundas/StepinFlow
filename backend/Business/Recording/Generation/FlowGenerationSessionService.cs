using Core.Enums;
using Core.Models.Dtos;
using Core.Ports;

namespace Business.Recording.Generation
{
    /// <summary>
    /// Owns one generation at a time: the model's draft of a recording, or a revision the Check page
    /// asked for. A model takes minutes over a draft - longer than a request can wait, on a pipe
    /// that answers one call at a time - so the request starts it and returns,
    /// FLOW_GENERATION_PROGRESS says how far, and GetResult has what was saved once it says DONE.
    /// A request shown before a cloud model is sent it waits here for Send, or for Cancel.
    /// </summary>
    public sealed class FlowGenerationSessionService : IFlowGenerationSessionService, IDisposable
    {
        private readonly IFlowGenerationService _generationService;
        private readonly IIpcBroadcastService _broadcastService;

        // A start and a cancel can come at the same moment, from two clicks.
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Lock _progressLock = new Lock();

        private Task? _generation;
        private CancellationTokenSource? _cts;
        private volatile FlowGenerationProgressDto _progress = new FlowGenerationProgressDto();
        private volatile FlowGenerationResultDto? _result;
        private volatile TaskCompletionSource _sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public FlowGenerationSessionService(IFlowGenerationService generationService, IIpcBroadcastService broadcastService)
        {
            _generationService = generationService;
            _broadcastService = broadcastService;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public Task<ResultDto<bool>> StartAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt)
        {
            return BeginAsync((onProgress, ct) => _generationService.ImportModelDraftAsync(flowId, recording, reviewed, screenAt, onProgress, WaitToSendAsync, ct));
        }

        public ResultDto<bool> Send()
        {
            lock (_progressLock)
            {
                if (_progress.State != FlowGenerationStateEnum.GENERATING || _progress.Stage != FlowGenerationStageEnum.WAITING_TO_SEND)
                    return ResultDto<bool>.Failure("No request is waiting to be sent.");

                _sent.TrySetResult();
            }

            return ResultDto<bool>.Success(true);
        }

        public Task<ResultDto<bool>> StartRevisionAsync(RecordingDraftRevisionDto revision, IReadOnlyList<string> screenLines)
        {
            return BeginAsync((onProgress, ct) => _generationService.ReviseAsync(revision.FlowId, revision.Changes, revision.Undecided, screenLines, onProgress, ct));
        }

        public FlowGenerationResultDto? GetResult()
        {
            return _result;
        }

        public async Task CancelAsync()
        {
            await _gate.WaitAsync();

            try
            {
                await StopAsync();
                _result = null;
                Report(new FlowGenerationProgressDto());
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<bool>> BeginAsync(Func<Action<FlowGenerationProgressDto>, CancellationToken, Task<FlowGenerationResultDto>> generate)
        {
            await _gate.WaitAsync();

            try
            {
                if (_progress.State == FlowGenerationStateEnum.GENERATING)
                    return ResultDto<bool>.Failure("The flow is already being generated.");

                await StopAsync();
                _result = null;

                CancellationTokenSource cts = new CancellationTokenSource();
                _cts = cts;
                _sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                // Not sent: the first stage says it, with the model and what it is sent. Kept, so a
                // second start is refused from now.
                lock (_progressLock)
                    _progress = new FlowGenerationProgressDto { State = FlowGenerationStateEnum.GENERATING, Stage = FlowGenerationStageEnum.WRITING_DRAFT };

                _generation = Task.Run(() => GenerateAsync(generate, cts.Token), CancellationToken.None);
                return ResultDto<bool>.Success(true);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task GenerateAsync(Func<Action<FlowGenerationProgressDto>, CancellationToken, Task<FlowGenerationResultDto>> generate, CancellationToken ct)
        {
            try
            {
                FlowGenerationResultDto result = await generate(Report, ct);

                _result = result;
                Report(Done(null));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Cancelled: the cancel says NONE once this has ended.
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Generation] Nothing was saved: {ex.Message}");
                Report(Done($"Nothing was saved. {ex.Message}"));
            }
        }

        // Until the tester sends it, or a cancel ends the generation.
        private Task WaitToSendAsync(CancellationToken ct)
        {
            return _sent.Task.WaitAsync(ct);
        }

        // What the last progress said about the model, with the end.
        private FlowGenerationProgressDto Done(string? errorMessage)
        {
            FlowGenerationProgressDto last = _progress;

            return new FlowGenerationProgressDto
            {
                State = FlowGenerationStateEnum.DONE,
                Model = last.Model,
                IsReadTextSent = last.IsReadTextSent,
                ScriptLength = last.ScriptLength,
                Attempt = last.Attempt,
                AttemptCount = last.AttemptCount,
                ErrorMessage = errorMessage,
            };
        }

        // Kept and sent under one lock, so two stages reported from two threads arrive in order.
        private void Report(FlowGenerationProgressDto progress)
        {
            lock (_progressLock)
            {
                _progress = progress;
                _ = _broadcastService.SendAsync(BroadcastTypeEnum.FLOW_GENERATION_PROGRESS, progress).AsTask();
            }
        }

        private async Task StopAsync()
        {
            CancellationTokenSource? cts = _cts;
            if (cts == null)
                return;

            await cts.CancelAsync();

            if (_generation != null)
                await _generation;

            cts.Dispose();
            _cts = null;
            _generation = null;
        }
    }
}
