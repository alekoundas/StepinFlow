using Business.AppSettings;
using Business.AreaPoint;
using Business.Recording.ActionBuilder;
using Business.Recording.Elements;
using Business.Recording.Models.Session;
using Business.Recording.Screens;
using Business.Recording.Setup;
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
    /// Owns one recording at a time: its start, pause, resume and stop, and the inputs between them.
    /// It collects the raw inputs (presses, releases, scrolls, keys) and the screenshot each press
    /// took, and has RecordingActionBuilder fold them into actions: a few times a second for the
    /// live list, and once more at the stop. It decides nothing about what the inputs mean.
    ///
    /// What it does beside that is each a part of its own, sharing the one cache:
    /// RecordingScreenCapture keeps the main area's last few screenshots, PressedElementLookups
    /// asks each press's application what it landed on, RecordingGestureAsker pauses at a gesture
    /// and asks what it is for, and RecordingProcessor works a stopped recording through.
    ///
    /// A press takes the newest screenshot from before it, picked at the hook, so its screenshot
    /// shows the screen the tester decided on rather than what the press changed. Encoding happens
    /// on the drain side, never in the hook handler, which must return at once.
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
    /// background and returns. RECORDING_PROGRESS says which stage and how far, then READY, and
    /// GetResult has it.
    /// </summary>
    public sealed class RecordingSessionService : IRecordingSessionService, IDisposable
    {
        // Long enough for the hook to hand over what it already queued, so the last real action
        // is not lost to the stop that follows it.
        private static readonly TimeSpan DRAIN_DELAY = TimeSpan.FromMilliseconds(150);

        // Often enough that the live list keeps up, and no more: a fold reads the whole recording.
        private static readonly TimeSpan FOLD_INTERVAL = TimeSpan.FromMilliseconds(250);

        private readonly IInputRecordService _inputRecordService;
        private readonly IScreenshotService _screenshotService;
        private readonly IAppSettingService _appSettingService;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly IWindowService _windowService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IClickedElementFinder _clickedElementFinder;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TimeProvider _timeProvider;

        private readonly RecordingInputFilter _filter;
        private readonly RecordingHotkeyReader _hotkeys = new RecordingHotkeyReader();
        private readonly RecordingSessionCache _cache = new RecordingSessionCache();
        private readonly RecordingScreenCapture _screens;
        private readonly PressedElementLookups _pressedElements;
        private readonly RecordingGestureAsker _gestureAsker;
        private readonly RecordingProcessor _processor;

        // The bar, a hotkey and the page can all stop a recording, at the same moment.
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Lock _progressLock = new Lock();

        // The gestures this recording holds back, from the settings when it began, and whether one
        // pauses it to ask what it is for.
        private IReadOnlyList<PressGesture> _gestures = [];
        private bool _isAskingAtGesture;

        private Channel<PendingInput>? _queue;
        private Task? _drain;
        private Task? _captureLoop;
        private Task? _foldLoop;
        private CancellationTokenSource? _loopsCts;

        public RecordingSessionService(
            IInputRecordService inputRecordService,
            IScreenshotService screenshotService,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcastService,
            IWindowService windowService,
            IAreaPointResolver areaPointResolver,
            IClickedElementFinder clickedElementFinder,
            IClickedElementGrouper clickedElementGrouper,
            IRecordedScreenReader recordedScreenReader,
            IAccessibleElementService accessibleElementService,
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
            _dbContextFactory = dbContextFactory;
            _timeProvider = timeProvider;

            _filter = new RecordingInputFilter(windowService);
            _screens = new RecordingScreenCapture(areaPointResolver, windowService, screenshotService, _cache, _filter, timeProvider);
            _pressedElements = new PressedElementLookups(accessibleElementService, _cache, timeProvider);
            _gestureAsker = new RecordingGestureAsker(_cache, _filter, windowService, clickedElementFinder, Pause, Resume, SendProgressAsync);
            _processor = new RecordingProcessor(_cache, _pressedElements, clickedElementFinder, clickedElementGrouper, recordedScreenReader, SendProgressAsync);
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

            // From the bar or its hotkey while a gesture asks: left for Review.
            _cache.Asking = null;

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

        // Nothing is typed for it while recording: the bar never takes the keyboard from the
        // application, and a click back into it would be recorded. Review asks for the name.
        public void MarkStage()
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null)
                return;

            RecordedInput stage = new RecordedInput
            {
                Type = RecordedInputTypeEnum.STAGE,
                CreatedOn = _timeProvider.GetLocalNow().DateTime,
            };

            queue.Writer.TryWrite(new PendingInput(stage, null));
        }

        public async Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);

            try
            {
                // Under the gate, straight after the stop, so nothing can start or discard in between.
                if (await EndAsync(ct))
                    await _processor.BeginAsync();

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
                await _processor.StopAsync();

                // A gesture asked about may have loaded the detector, and no stop will let it go.
                _clickedElementFinder.Unload();

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
                ScreenChanges = _cache.ScreenChanges.ToList(),
                ScreenPages = _cache.ScreenPages.ToList(),
                ScreenLines = _cache.ScreenLines.ToList(),
                PressedElements = _pressedElements.ElementsOf(_cache.Actions),
                GestureAnswers = _gestureAsker.AnswersOf(_cache.Actions),
            };
        }

        public RecordingGestureDto? GetGesture()
        {
            return _gestureAsker.GetAsked();
        }

        public ResultDto<bool> DecideGesture(RecordingGestureDecisionDto decision)
        {
            return _gestureAsker.Decide(decision);
        }

        public bool TestGestureRule(RecordingGestureRuleDto rule)
        {
            return _gestureAsker.TestRule(rule);
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

        public void Dispose()
        {
            _inputRecordService.ActionRecorded -= OnActionRecorded;
            _queue?.Writer.TryComplete();
            _queue = null;
            _loopsCts?.Cancel();
            _processor.Cancel();
        }


        // ================================================================
        // Private methods - start and stop
        // ================================================================

        private async Task<ResultDto<bool>> BeginAsync(int flowId, int electronProcessId, CancellationToken ct)
        {
            if (IsRecording)
                return ResultDto<bool>.Failure("A recording is already running.");

            // Without it the Stop click, and anything typed into StepinFlow, would be recorded.
            if (electronProcessId <= 0)
                return ResultDto<bool>.Failure("StepinFlow could not tell which windows are its own.");

            await _processor.StopAsync();
            _cache.Clear();
            _filter.Reset();
            _filter.ElectronProcessId = electronProcessId;

            _pressedElements.Reset();
            _gestureAsker.Reset();
            _hotkeys.Reset();

            string pauseKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyPause, ct);
            string stopKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyStop, ct);
            _hotkeys.Bind(RecordingHotkeyEnum.PAUSE, pauseKeys);
            _hotkeys.Bind(RecordingHotkeyEnum.STOP, stopKeys);

            _gestures = await GesturesAsync(ct);
            _hotkeys.BindGestures(_gestures);

            string ask = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingGestureAsk, ct);
            _isAskingAtGesture = !bool.TryParse(ask, out bool isAsking) || isAsking;

            FlowArea? mainArea = await MainAreaOfAsync(flowId, ct);
            if (mainArea == null)
                return ResultDto<bool>.Failure("Pick the window the flow works in before recording.");

            string? notSized = await SizeMainWindowAsync(flowId, mainArea, ct);
            if (notSized != null)
                return ResultDto<bool>.Failure(notSized);

            // The first screenshot is taken before the first press can happen.
            _screens.Reset();
            string? notOnScreen = _screens.Capture(mainArea);
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
            _foldLoop = Task.Run(() => FoldLoopAsync(loopsCts.Token), CancellationToken.None);
            _captureLoop = Task.Run(() => _screens.LoopAsync(mainArea, loopsCts.Token), CancellationToken.None);

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

        // The size the flow's first resize of its main window sets, set again here: the tester may
        // have changed it while arranging their windows, and the recording is made at that size.
        // A window not on screen is left to the first screenshot to say so.
        private async Task<string?> SizeMainWindowAsync(int flowId, FlowArea mainArea, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowStep? resize = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.RootId == flowId && x.ParentFlowStepId == null)
                .Where(x => x.FlowStepType == FlowStepTypeEnum.WINDOW_RESIZE && x.FlowAreaId == mainArea.Id)
                .OrderBy(x => x.OrderNumber)
                .FirstOrDefaultAsync(ct);

            if (resize == null || resize.WindowWidth < 1 || resize.WindowHeight < 1)
                return null;

            AreaResolution area = _areaPointResolver.ResolveArea(mainArea);
            if (!area.IsResolved)
                return null;

            string? refused = RecordingWindowSize.Apply(_windowService, area.Window, resize.WindowWidth, resize.WindowHeight);
            if (refused != null)
                return $"{refused} \"{resize.Name}\" sets it, and the recording is made at that size. Restore the window if it is maximised or full screen, then start again.";

            return null;
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

            // Stopped while a gesture asked: left for Review.
            _cache.Asking = null;

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

            // What the last action did shows on the screen it was left on. Taken while the bar was
            // still up, so StepinFlow's own window is not over it.
            _cache.LastScreenshot = _cache.GetRecentScreenshotBefore(_timeProvider.GetLocalNow().DateTime);

            // The inputs and the screenshots per press stay for the review.
            _filter.Reset();
            _hotkeys.Reset();
            _cache.ClearRecentScreenshots();

            Fold();
            return true;
        }

        private async Task StopLoopsAsync()
        {
            CancellationTokenSource? loopsCts = _loopsCts;
            if (loopsCts == null)
                return;

            await loopsCts.CancelAsync();

            if (_captureLoop != null)
                await _captureLoop;

            if (_foldLoop != null)
                await _foldLoop;

            loopsCts.Dispose();
            _loopsCts = null;
            _captureLoop = null;
            _foldLoop = null;
        }


        // ================================================================
        // Private methods - from the hook to the cache
        // ================================================================

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

        // Only a press carries a screenshot, and only a press is asked what it landed on.
        private void Handle(PendingInput pending)
        {
            int index = _cache.AddInput(pending.Input);

            if (pending.Screenshot != null)
                KeepPress(index, pending.Input, pending.Screenshot);

            if (pending.Input.Type == RecordedInputTypeEnum.BUTTON_DOWN)
                _pressedElements.Ask(index, pending.Input);

            if (pending.Input.Type == RecordedInputTypeEnum.BUTTON_DOWN && pending.Input.Gesture != null && _isAskingAtGesture)
                _gestureAsker.Ask(index, pending.Input, pending.Input.Gesture.Value);
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


        // ================================================================
        // Private methods - the live list
        // ================================================================

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


        // ================================================================
        // Private types
        // ================================================================

        // An input on its way from the hook to the drain, with the screenshot a press took at the
        // hook. Held by reference, so the drain can encode it after the cache has moved on.
        private sealed record PendingInput(RecordedInput Input, RecordingScreenshot? Screenshot);
    }
}
