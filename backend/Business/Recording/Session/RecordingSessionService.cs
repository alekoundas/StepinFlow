using Business.AppSettings;
using Business.AreaPoint;
using Business.Recording.ActionBuilder;
using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Core.Catalogs;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace Business.Recording.Session
{
    /// <summary>
    /// Owns one recording at a time.
    /// It collects the raw inputs (presses, releases, scrolls, keys) and the screenshots taken around them,
    /// and has RecordingActionBuilder fold them into actions: a few times a second for the live list, and once more at the stop.
    /// It decides nothing about what the inputs mean.
    ///
    /// While recording, the main area is captured a few times a second and the last few kept. A
    /// press takes the newest from before it, picked at the hook, so its screenshot shows the
    /// screen the tester decided on rather than what the press changed. Encoding happens on the
    /// drain side, never in the hook handler, which must return at once.
    ///
    /// The recording bar's hotkeys are read at the hook before anything else sees the input, so
    /// pressing one is never recorded, whatever window is in front.
    ///
    /// The queue belongs to the session and is created with it, so a recording can never inherit
    /// events from before it started. Stopping completes the writer rather than cancelling the
    /// reader: a completed channel ends the read normally, where a cancelled one throws on every
    /// stop and leaves a single reader channel unusable for everything after it.
    ///
    /// Every change, a fold or a pause, is broadcast whole as RECORDING_PROGRESS. Pixels never go
    /// on it: the review asks for an image when it draws that action.
    ///
    /// A stop, from the page, the bar or its hotkey, starts working the recording through in the
    /// background, stage by stage, and returns: finding what each click hit is seconds per click,
    /// and the request pipe answers one call at a time. Never while recording, where the
    /// application being recorded would feel it. RECORDING_PROGRESS says which stage and how far,
    /// then READY, and GetResult has it.
    /// </summary>
    public sealed class RecordingSessionService : IRecordingSessionService, IDisposable
    {
        // Long enough for the hook to hand over what it already queued, so the last real action
        // is not lost to the stop that follows it.
        private static readonly TimeSpan DRAIN_DELAY = TimeSpan.FromMilliseconds(150);

        // Often enough that the newest screenshot is from just before a press, and no more often:
        // a capture is real work beside the application being recorded.
        private static readonly TimeSpan CAPTURE_INTERVAL = TimeSpan.FromMilliseconds(250);

        // Often enough that the live list keeps up, and no more: a fold reads the whole recording.
        private static readonly TimeSpan FOLD_INTERVAL = TimeSpan.FromMilliseconds(250);

        // What a stopped recording is worked through, in order.
        private static readonly RecordingStageEnum[] STAGES = [RecordingStageEnum.FINDING_ELEMENTS, RecordingStageEnum.GROUPING_CLICKS];

        private readonly IInputRecordService _inputRecordService;
        private readonly IScreenshotService _screenshotService;
        private readonly IAppSettingService _appSettingService;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly IWindowService _windowService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IClickedElementFinder _clickedElementFinder;
        private readonly IClickedElementGrouper _clickedElementGrouper;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TimeProvider _timeProvider;

        private readonly RecordingInputFilter _filter;
        private readonly RecordingHotkeyReader _hotkeys = new RecordingHotkeyReader();
        private readonly RecordingSessionCache _cache = new RecordingSessionCache();

        // The bar, a hotkey and the page can all stop a recording, at the same moment.
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Lock _progressLock = new Lock();

        // The gestures this recording holds back, from the settings when it began.
        private IReadOnlyList<PressGesture> _gestures = [];

        private Channel<PendingInput>? _queue;
        private Task? _drain;
        private Task? _capture;
        private Task? _fold;
        private CancellationTokenSource? _loopsCts;
        private Task? _processing;
        private CancellationTokenSource? _processingCts;

        public RecordingSessionService(
            IInputRecordService inputRecordService,
            IScreenshotService screenshotService,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcastService,
            IWindowService windowService,
            IAreaPointResolver areaPointResolver,
            IClickedElementFinder clickedElementFinder,
            IClickedElementGrouper clickedElementGrouper,
            IDbContextFactory<AppDbContext> dbContextFactory,
            TimeProvider timeProvider)
        {
            _inputRecordService = inputRecordService;
            _screenshotService = screenshotService;
            _appSettingService = appSettingService;
            _broadcastService = broadcastService;
            _windowService = windowService;
            _areaPointResolver = areaPointResolver;
            _clickedElementFinder = clickedElementFinder;
            _clickedElementGrouper = clickedElementGrouper;
            _dbContextFactory = dbContextFactory;
            _timeProvider = timeProvider;

            _filter = new RecordingInputFilter(windowService);
        }

        public bool IsRecording { get { return _queue != null; } }
        public bool IsPaused { get { return _filter.IsPaused; } }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<bool>> StartAsync(int flowId, int electronProcessId, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);

            try
            {
                return await BeginAsync(flowId, electronProcessId, ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Pause()
        {
            if (!IsRecording || _filter.IsPaused)
                return;

            // Paused, a Ctrl + click is the tester's own and reaches the application.
            _inputRecordService.HoldBackPresses([]);
            _filter.IsPaused = true;
            _cache.State = RecordingStateEnum.PAUSED;
            _ = SendProgressAsync();
        }

        public void Resume()
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null || !_filter.IsPaused)
                return;

            // Marked, so the time spent paused is not read as a wait the flow has to make.
            RecordedInput resumed = new RecordedInput
            {
                Type = RecordedInputTypeEnum.RESUMED,
                CreatedOn = _timeProvider.GetLocalNow().DateTime,
            };

            queue.Writer.TryWrite(new PendingInput(resumed, null));
            _inputRecordService.HoldBackPresses(_gestures);
            _filter.IsPaused = false;
            _cache.State = RecordingStateEnum.RECORDING;
            _ = SendProgressAsync();
        }

        public async Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);

            try
            {
                if (await EndAsync(ct))
                    await BeginProcessingAsync();

                return _cache.GetInputs();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task DiscardAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);

            try
            {
                await EndAsync(ct);
                await StopProcessingAsync();
                _cache.Clear();
                await SendProgressAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        public RecordingResultDto? GetResult()
        {
            if (_cache.State != RecordingStateEnum.READY)
                return null;

            return new RecordingResultDto
            {
                Actions = _cache.Actions.ToList(),
                ClickedElements = _cache.ClickedElements.ToList(),
                ClickGroups = _cache.ClickGroups.ToList(),
            };
        }

        public RecordingScreenDto? GetScreen(int actionIndex)
        {
            IReadOnlyList<RecordedActionDto> actions = _cache.Actions;

            for (int i = actions.Count - 1; i >= 0; i--)
            {
                RecordedActionDto action = actions[i];
                if (action.Index > actionIndex || action.ScreenshotIndex == null)
                    continue;

                RecordedPress? press = _cache.GetPress(action.ScreenshotIndex.Value);
                if (press == null)
                    continue;

                return new RecordingScreenDto
                {
                    ActionIndex = action.Index,
                    Image = press.Screenshot,
                    X = press.MainAreaBounds.X,
                    Y = press.MainAreaBounds.Y,
                    Width = press.MainAreaBounds.Width,
                    Height = press.MainAreaBounds.Height,
                    Dpi = press.Dpi,
                };
            }

            return null;
        }

        public ResultDto<bool> Review(ReviewedRecordingDto reviewed)
        {
            if (_cache.State != RecordingStateEnum.READY)
                return ResultDto<bool>.Failure("The recording is not ready to review yet.");

            HashSet<int> recorded = _cache.Actions.Select(x => x.Index).ToHashSet();
            if (reviewed.Actions.Any(x => !recorded.Contains(x.ActionIndex)))
                return ResultDto<bool>.Failure("The review names an action the recording does not have.");

            _cache.Reviewed = reviewed;
            return ResultDto<bool>.Success(true);
        }

        public ReviewedRecordingDto? GetReviewed()
        {
            return _cache.Reviewed;
        }

        public RecordedPress? GetPress(int index)
        {
            return _cache.GetPress(index);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<bool>> BeginAsync(int flowId, int electronProcessId, CancellationToken ct)
        {
            if (IsRecording)
                return ResultDto<bool>.Failure("A recording is already running.");

            // Without it the Stop click, and anything typed into StepinFlow, would be recorded.
            if (electronProcessId <= 0)
                return ResultDto<bool>.Failure("StepinFlow could not tell which windows are its own.");

            await StopProcessingAsync();
            _cache.Clear();
            _filter.Reset();
            _filter.ElectronProcessId = electronProcessId;
            _hotkeys.Reset();

            string pauseKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyPause, ct);
            string stopKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyStop, ct);
            _hotkeys.Bind(RecordingHotkeyEnum.PAUSE, pauseKeys);
            _hotkeys.Bind(RecordingHotkeyEnum.STOP, stopKeys);

            _gestures = await GesturesAsync(ct);
            _hotkeys.BindGestures(_gestures);

            FlowArea? mainArea = await MainAreaOfAsync(flowId, ct);
            if (mainArea == null)
                return ResultDto<bool>.Failure("Pick the window the flow works in before recording.");

            // The first screenshot is taken before the first press can happen.
            string? notOnScreen = CaptureScreenshot(mainArea);
            if (notOnScreen != null)
                return ResultDto<bool>.Failure(notOnScreen);

            if (!await _inputRecordService.StartRecordingAllAsync())
                return ResultDto<bool>.Failure("Input is being captured for something else. Close it and start again.");

            _inputRecordService.HoldBackPresses(_gestures);

            _cache.StartedOn = _timeProvider.GetLocalNow().DateTime;
            _cache.State = RecordingStateEnum.RECORDING;

            Channel<PendingInput> queue = Channel.CreateUnbounded<PendingInput>(new UnboundedChannelOptions { SingleReader = true });

            _queue = queue;
            _drain = Task.Run(() => DrainAsync(queue), CancellationToken.None);
            _inputRecordService.ActionRecorded += OnActionRecorded;

            CancellationTokenSource loopsCts = new CancellationTokenSource();
            _loopsCts = loopsCts;
            _fold = Task.Run(() => FoldLoopAsync(loopsCts.Token), CancellationToken.None);
            _capture = Task.Run(() => CaptureLoopAsync(mainArea, loopsCts.Token), CancellationToken.None);

            await SendProgressAsync();
            return ResultDto<bool>.Success(true);
        }

        // An unbound or unreadable one is no gesture: its presses are clicks.
        private async Task<IReadOnlyList<PressGesture>> GesturesAsync(CancellationToken ct)
        {
            List<PressGesture> gestures = [];

            foreach (GestureAppSettingDefinition definition in new[] { AppSettingCatalog.RecordingGestureCheck, AppSettingCatalog.RecordingGestureSubstitute })
            {
                string combination = await _appSettingService.GetTextAsync(definition, ct);
                PressGesture? gesture = PressGesture.Parse(definition.Gesture, combination);
                if (gesture != null)
                    gestures.Add(gesture);
            }

            return gestures;
        }

        private async Task<FlowArea?> MainAreaOfAsync(int flowId, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            return await dbContext.FlowAreas.AsNoTracking().FirstOrDefaultAsync(x => x.FlowId == flowId && x.IsMain, ct);
        }

        // Ends a recording that is running, and folds what it recorded. False when none was.
        private async Task<bool> EndAsync(CancellationToken ct)
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null)
                return false;

            await _inputRecordService.StopRecordingAllAsync();
            _inputRecordService.ActionRecorded -= OnActionRecorded;
            _cache.EndedOn = _timeProvider.GetLocalNow().DateTime;

            // The hook stops publishing immediately but may still be inside a handler, so the
            // queue stays open long enough for those to land.
            await Task.Delay(DRAIN_DELAY, ct);

            _queue = null;
            queue.Writer.Complete();

            if (_drain != null)
            {
                await _drain;
                _drain = null;
            }

            await StopLoopsAsync();

            // The inputs and the screenshots per press stay for the review.
            _filter.Reset();
            _hotkeys.Reset();
            _cache.ClearRecentScreenshots();

            Fold();
            return true;
        }

        // On the hook's event thread, which every input after this one waits on. Nothing here may
        // block, a stop included.
        private void OnActionRecorded(RecordedInput input)
        {
            if (_queue == null)
                return;

            IReadOnlyList<RecordedInput> released = _hotkeys.Read(input, out RecordingHotkeyEnum? hotkey);
            foreach (RecordedInput each in released)
                Record(each);

            if (hotkey != null)
                Press(hotkey.Value);
        }

        // A window lookup, and a screenshot picked by reference so the drain can encode it after
        // the cache has moved on.
        private void Record(RecordedInput input)
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null || !_filter.Keep(input))
                return;

            RecordingScreenshot? screenshot = null;
            if (input.Type == RecordedInputTypeEnum.BUTTON_DOWN)
                screenshot = _cache.GetRecentScreenshotBefore(input.CreatedOn);

            queue.Writer.TryWrite(new PendingInput(input, screenshot));
        }

        private void Press(RecordingHotkeyEnum hotkey)
        {
            switch (hotkey)
            {
                case RecordingHotkeyEnum.PAUSE:
                    if (IsPaused)
                        Resume();
                    else
                        Pause();
                    break;

                case RecordingHotkeyEnum.STOP:
                    _ = Task.Run(StopFromHotkeyAsync);
                    break;
            }
        }

        private async Task StopFromHotkeyAsync()
        {
            try
            {
                await StopAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Recording] The stop key could not stop the recording: {ex.Message}");
            }
        }

        private async Task DrainAsync(Channel<PendingInput> queue)
        {
            await foreach (PendingInput pending in queue.Reader.ReadAllAsync())
            {
                // One bad action must not end the drain. It is the only reader, so if it dies the
                // rest of the recording is silently lost.
                try
                {
                    Handle(pending);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Recording] Dropped an action: {ex.Message}");
                }
            }
        }

        // Only a press carries a screenshot.
        private void Handle(PendingInput pending)
        {
            int index = _cache.AddInput(pending.Input);

            if (pending.Screenshot != null)
                KeepPress(index, pending.Input, pending.Screenshot);
        }

        // The whole main area, which Review shows and the draft cuts its templates from. A failed
        // encode costs the review a picture, never the recording.
        private void KeepPress(int index, RecordedInput input, RecordingScreenshot screenshot)
        {
            try
            {
                byte[] encodedMainArea = _screenshotService.Encode(screenshot.Image, ScreenshotFormatEnum.PNG, 100);
                _cache.AddPress(new RecordedPress
                {
                    Index = index,
                    MainAreaBounds = screenshot.Target.Bounds,
                    Dpi = screenshot.Target.Dpi,
                    Screenshot = encodedMainArea,
                });

                input.HasScreenshot = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Recording] Kept a press without its screenshot: {ex.Message}");
            }
        }

        // Null when it worked, else why not.
        private string? CaptureScreenshot(FlowArea mainArea)
        {
            AreaResolution area = _areaPointResolver.ResolveArea(mainArea);
            if (!area.IsResolved)
                return area.Error;

            int processId = _windowService.GetProcessId(area.Window);
            RecordingTarget target = new RecordingTarget(area.Bounds, area.Dpi, area.Window, processId);
            RawImage image = _screenshotService.CaptureRaw(area.Bounds);
            DateTime capturedOn = _timeProvider.GetLocalNow().DateTime;

            _cache.AddRecentScreenshot(new RecordingScreenshot(capturedOn, target, image));
            _filter.Target = target;
            return null;
        }

        // A main area minimised or closed for a moment keeps the screenshots from before it,
        // rather than ending the recording.
        private async Task CaptureLoopAsync(FlowArea mainArea, CancellationToken ct)
        {
            using PeriodicTimer timer = new PeriodicTimer(CAPTURE_INTERVAL, _timeProvider);

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        CaptureScreenshot(mainArea);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Recording] Missed a screenshot: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped.
            }
        }

        // Folds only when something new arrived, so a tester reading the screen costs nothing.
        private async Task FoldLoopAsync(CancellationToken ct)
        {
            using PeriodicTimer timer = new PeriodicTimer(FOLD_INTERVAL, _timeProvider);
            int foldedCount = 0;

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    int count = _cache.InputCount;
                    if (count == foldedCount)
                        continue;

                    foldedCount = count;

                    try
                    {
                        Fold();
                        await SendProgressAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Recording] Missed a fold of the live list: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stopped.
            }
        }

        // Only ever on one thread at a time - the fold loop, or the stop once it has ended -
        // because folding writes what each key typed onto its key-down.
        private void Fold()
        {
            IReadOnlyList<RecordedInput> inputs = _cache.GetInputs();
            _cache.Actions = RecordingActionBuilder.Build(inputs);
        }

        // Built and sent under one lock, so a fold and a pause sent from two threads arrive in the
        // order they happened.
        private Task SendProgressAsync()
        {
            lock (_progressLock)
            {
                DateTime now = _timeProvider.GetLocalNow().DateTime;
                RecordingProgressDto progress = RecordingProgressBuilder.Build(_cache, now);
                return _broadcastService.SendAsync(BroadcastTypeEnum.RECORDING_PROGRESS, progress).AsTask();
            }
        }

        // Under the gate, straight after the stop, so nothing can start or discard in between.
        private async Task BeginProcessingAsync()
        {
            IReadOnlyList<RecordedActionDto> actions = _cache.Actions;

            _cache.State = RecordingStateEnum.PROCESSING;
            _cache.Stage = StageProgress(STAGES[0], 0, null);
            await SendProgressAsync();

            CancellationTokenSource processingCts = new CancellationTokenSource();
            _processingCts = processingCts;
            _processing = Task.Run(() => ProcessAsync(actions, processingCts.Token), CancellationToken.None);
        }

        // A stage that fails costs the review what it would have added, never the recording.
        private async Task ProcessAsync(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            try
            {
                await FindElementsAsync(actions, ct);
                GroupClicks(actions, ct);
            }
            catch (OperationCanceledException)
            {
                // Discarded, or a new recording started.
                return;
            }

            _cache.Stage = null;
            _cache.State = RecordingStateEnum.READY;
            await SendProgressAsync();
        }

        private async Task FindElementsAsync(IReadOnlyList<RecordedActionDto> actions, CancellationToken ct)
        {
            try
            {
                _cache.ClickedElements = await _clickedElementFinder.FindAsync(actions, _cache.GetPress, (done, total) =>
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

        private void ReportStage(RecordingStageEnum stage, int done, int? total)
        {
            _cache.Stage = StageProgress(stage, done, total);
            _ = SendProgressAsync();
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

        private async Task StopProcessingAsync()
        {
            CancellationTokenSource? processingCts = _processingCts;
            if (processingCts == null)
                return;

            await processingCts.CancelAsync();

            if (_processing != null)
                await _processing;

            processingCts.Dispose();
            _processingCts = null;
            _processing = null;
        }

        private async Task StopLoopsAsync()
        {
            CancellationTokenSource? loopsCts = _loopsCts;
            if (loopsCts == null)
                return;

            await loopsCts.CancelAsync();

            if (_capture != null)
                await _capture;

            if (_fold != null)
                await _fold;

            loopsCts.Dispose();
            _loopsCts = null;
            _capture = null;
            _fold = null;
        }

        public void Dispose()
        {
            _inputRecordService.ActionRecorded -= OnActionRecorded;
            _queue?.Writer.TryComplete();
            _queue = null;
            _loopsCts?.Cancel();
            _processingCts?.Cancel();
        }


        // ================================================================
        // Private types
        // ================================================================

        // An input on its way from the hook to the drain, with the screenshot a press took at the
        // hook. Held by reference, so the drain can encode it after the cache has moved on.
        private sealed record PendingInput(RecordedInput Input, RecordingScreenshot? Screenshot);
    }
}
