using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Business.Recording.Screens;
using Core.Enums;
using Core.Models.Dtos;

namespace Business.Recording.Session
{
    /// <summary>
    /// A stopped recording worked through in the background, stage by stage: what each click hit,
    /// which clicks hit the same picture, then what changed on the screen. Finding what a click hit
    /// is seconds per click, and the request pipe answers one call at a time, so the stop starts
    /// this and returns. Never while recording, where the application being recorded would feel it.
    /// Each stage says how far it is, then the recording is READY.
    ///
    /// A stage that fails costs the review what it would have added, never the recording.
    /// </summary>
    public sealed class RecordingProcessor
    {
        // What a stopped recording is worked through, in order.
        private static readonly RecordingStageEnum[] STAGES = [RecordingStageEnum.FINDING_ELEMENTS, RecordingStageEnum.GROUPING_CLICKS, RecordingStageEnum.READING_SCREENS];

        private readonly RecordingSessionCache _cache;
        private readonly PressedElementLookups _pressedElements;
        private readonly IClickedElementFinder _clickedElementFinder;
        private readonly IClickedElementGrouper _clickedElementGrouper;
        private readonly IRecordedScreenReader _recordedScreenReader;
        private readonly Func<Task> _sendProgress;

        private Task? _processing;
        private CancellationTokenSource? _cts;

        public RecordingProcessor(
            RecordingSessionCache cache,
            PressedElementLookups pressedElements,
            IClickedElementFinder clickedElementFinder,
            IClickedElementGrouper clickedElementGrouper,
            IRecordedScreenReader recordedScreenReader,
            Func<Task> sendProgress)
        {
            _cache = cache;
            _pressedElements = pressedElements;
            _clickedElementFinder = clickedElementFinder;
            _clickedElementGrouper = clickedElementGrouper;
            _recordedScreenReader = recordedScreenReader;
            _sendProgress = sendProgress;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>Starts working the recording's actions through, and returns.</summary>
        public async Task BeginAsync()
        {
            IReadOnlyList<RecordedActionDto> actions = _cache.Actions;

            _cache.State = RecordingStateEnum.PROCESSING;
            _cache.Stage = StageProgress(STAGES[0], 0, null);
            await _sendProgress();

            CancellationTokenSource cts = new CancellationTokenSource();
            _cts = cts;
            _processing = Task.Run(() => ProcessAsync(actions, cts.Token), CancellationToken.None);
        }

        /// <summary>Stops one underway, and waits until it has.</summary>
        public async Task StopAsync()
        {
            CancellationTokenSource? cts = _cts;
            if (cts == null)
                return;

            await cts.CancelAsync();

            if (_processing != null)
                await _processing;

            cts.Dispose();
            _cts = null;
            _processing = null;
        }

        /// <summary>Stops one underway without waiting, as the app closes.</summary>
        public void Cancel()
        {
            _cts?.Cancel();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task ProcessAsync(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            try
            {
                await _pressedElements.AwaitAsync(ct);
                await FindElementsAsync(actions, ct);
                GroupClicks(actions, ct);
                await ReadScreensAsync(actions, ct);
            }
            catch (OperationCanceledException)
            {
                // Discarded, or a new recording started.
                return;
            }

            _cache.Stage = null;
            _cache.State = RecordingStateEnum.READY;
            await _sendProgress();
        }

        private async Task FindElementsAsync(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            try
            {
                _cache.ClickedElements = await _clickedElementFinder.FindAsync(actions, _cache.GetPress, _cache.GetGestureElements(), (done, total) =>
                {
                    ReportStage(RecordingStageEnum.FINDING_ELEMENTS, done, total);
                }, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Fail("Could not find what the clicks hit.", ex);
            }
        }

        // Compares the clicked elements, so it comes after finding them.
        private void GroupClicks(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            ReportStage(RecordingStageEnum.GROUPING_CLICKS, 0, null);

            try
            {
                _cache.ClickGroups = _clickedElementGrouper.Group(_cache.ClickedElements, actions, _cache.GetPress, (done, total) =>
                {
                    ReportStage(RecordingStageEnum.GROUPING_CLICKS, done, total);
                }, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Fail("Could not group the clicks that hit the same picture.", ex);
            }
        }

        // Last, since the line under a click is looked for by what the click hit.
        private async Task ReadScreensAsync(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            ReportStage(RecordingStageEnum.READING_SCREENS, 0, null);

            try
            {
                IReadOnlyList<ClickedElementDto> elements = _cache.ClickedElements;
                RecordedScreens screens = await _recordedScreenReader.ReadAsync(actions, _cache.GetPress, elements, _cache.LastScreenshot, (done, total) =>
                {
                    ReportStage(RecordingStageEnum.READING_SCREENS, done, total);
                }, ct);

                foreach (ClickedElementDto element in elements)
                    element.LineText = screens.LineTexts.GetValueOrDefault(element.ActionIndex) ?? string.Empty;

                _cache.ScreenChanges = screens.Changes;
                _cache.ScreenPages = screens.Pages;
                _cache.ScreenLines = screens.Lines;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Fail("Could not read the text on the screens.", ex);
            }

            // Kept whole rather than encoded as a press is, so it goes once it is read.
            _cache.LastScreenshot = null;
        }

        private void ReportStage(RecordingStageEnum stage, int done, int? total)
        {
            _cache.Stage = StageProgress(stage, done, total);
            _ = _sendProgress();
        }

        // The first failure is the one said: the ones after it are usually its consequence.
        private void Fail(string message, Exception ex)
        {
            Console.Error.WriteLine($"[Recording] {message} {ex.Message}");
            _cache.ErrorMessage ??= message;
        }

        private static RecordingStageProgress StageProgress(RecordingStageEnum stage, int done, int? total)
        {
            int number = Array.IndexOf(STAGES, stage) + 1;

            return new RecordingStageProgress
            {
                Stage = stage,
                Number = number,
                Count = STAGES.Length,
                Done = done,
                Total = total,
            };
        }
    }
}
