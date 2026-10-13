using Business.AppSettings;
using Business.AreaPoint;
using Business.Recording.ActionBuilder;
using Business.Recording.Models.Session;
using Core.Catalogs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Ports;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
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

        private readonly IInputRecordService _inputRecordService;
        private readonly IScreenshotService _screenshotService;
        private readonly IAppSettingService _appSettingService;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly IWindowService _windowService;
        private readonly IScreenService _screenService;
        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TimeProvider _timeProvider;

        private readonly RecordingInputFilter _filter;
        private readonly RecordingHotkeyReader _hotkeys = new RecordingHotkeyReader();
        private readonly RecordingSessionCache _cache = new RecordingSessionCache();

        // The bar, a hotkey and the page can all stop a recording, at the same moment.
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly Lock _progressLock = new Lock();

        private Channel<PendingInput>? _queue;
        private Task? _drain;
        private Task? _capture;
        private Task? _fold;
        private CancellationTokenSource? _loopsCts;

        public RecordingSessionService(
            IInputRecordService inputRecordService,
            IScreenshotService screenshotService,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcastService,
            IWindowService windowService,
            IScreenService screenService,
            IAreaPointResolver areaPointResolver,
            IDbContextFactory<AppDbContext> dbContextFactory,
            TimeProvider timeProvider)
        {
            _inputRecordService = inputRecordService;
            _screenshotService = screenshotService;
            _appSettingService = appSettingService;
            _broadcastService = broadcastService;
            _windowService = windowService;
            _screenService = screenService;
            _areaPointResolver = areaPointResolver;
            _dbContextFactory = dbContextFactory;
            _timeProvider = timeProvider;

            _filter = new RecordingInputFilter(windowService);
        }

        public bool IsRecording { get { return _queue != null; } }
        public bool IsPaused { get { return _filter.IsPaused; } }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<bool>> StartAsync(int? flowId, int electronProcessId, CancellationToken ct = default)
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
                {
                    _cache.State = RecordingStateEnum.STOPPED;
                    await SendProgressAsync();
                }

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
                _cache.Clear();
                await SendProgressAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        public IReadOnlyList<RecordedActionDto> GetActions()
        {
            return _cache.Actions;
        }

        public RecordingProgressDto GetProgress()
        {
            DateTime now = _timeProvider.GetLocalNow().DateTime;
            return RecordingProgressBuilder.Build(_cache.State, _cache.StartedOn, now, _cache.Actions);
        }

        public byte[]? GetScreenshot(int index)
        {
            return _cache.GetScreenshot(index);
        }

        public RecordedPress? GetPress(int index)
        {
            return _cache.GetPress(index);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<ResultDto<bool>> BeginAsync(int? flowId, int electronProcessId, CancellationToken ct)
        {
            if (IsRecording)
                return ResultDto<bool>.Failure("A recording is already running.");

            // Without it the Stop click, and anything typed into StepinFlow, would be recorded.
            if (electronProcessId <= 0)
                return ResultDto<bool>.Failure("StepinFlow could not tell which windows are its own.");

            _cache.Clear();
            _filter.Reset();
            _filter.ElectronProcessId = electronProcessId;
            _hotkeys.Reset();

            // Read once per session. Changing the capture size midway would leave the screenshots
            // inconsistent with each other for no benefit.
            int captureWidth = await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureWidth, ct);
            int captureHeight = await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureHeight, ct);
            _cache.CaptureSize = new Size(captureWidth, captureHeight);

            string pauseKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyPause, ct);
            string stopKeys = await _appSettingService.GetTextAsync(AppSettingCatalog.RecordingHotkeyStop, ct);
            _hotkeys.Bind(RecordingHotkeyEnum.PAUSE, pauseKeys);
            _hotkeys.Bind(RecordingHotkeyEnum.STOP, stopKeys);

            FlowArea? mainArea = null;
            if (flowId != null)
            {
                await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

                mainArea = await dbContext.FlowAreas.AsNoTracking().FirstOrDefaultAsync(x => x.FlowId == flowId && x.IsMain, ct);

                if (mainArea == null)
                    return ResultDto<bool>.Failure("Pick the window the flow works in before recording.");

                // The first screenshot is taken before the first press can happen.
                string? notOnScreen = CaptureScreenshot(mainArea);
                if (notOnScreen != null)
                    return ResultDto<bool>.Failure(notOnScreen);
            }

            if (!await _inputRecordService.StartRecordingAllAsync())
                return ResultDto<bool>.Failure("Input is being captured for something else. Close it and start again.");

            _cache.MainArea = mainArea;
            _cache.StartedOn = _timeProvider.GetLocalNow().DateTime;
            _cache.State = RecordingStateEnum.RECORDING;

            Channel<PendingInput> queue = Channel.CreateUnbounded<PendingInput>(new UnboundedChannelOptions { SingleReader = true });

            _queue = queue;
            _drain = Task.Run(() => DrainAsync(queue), CancellationToken.None);
            _inputRecordService.ActionRecorded += OnActionRecorded;

            CancellationTokenSource loopsCts = new CancellationTokenSource();
            _loopsCts = loopsCts;
            _fold = Task.Run(() => FoldLoopAsync(loopsCts.Token), CancellationToken.None);

            if (mainArea != null)
                _capture = Task.Run(() => CaptureLoopAsync(mainArea, loopsCts.Token), CancellationToken.None);

            await SendProgressAsync();
            return ResultDto<bool>.Success(true);
        }

        // Ends a recording that is running, and folds what it recorded. False when none was.
        private async Task<bool> EndAsync(CancellationToken ct)
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null)
                return false;

            await _inputRecordService.StopRecordingAllAsync();
            _inputRecordService.ActionRecorded -= OnActionRecorded;

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
            _cache.MainArea = null;

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

        private void Handle(PendingInput pending)
        {
            RecordedInput input = pending.Input;
            int index = _cache.AddInput(input);

            if (input.Type != RecordedInputTypeEnum.BUTTON_DOWN)
                return;

            if (pending.Screenshot != null)
                KeepPress(index, input, pending.Screenshot);
            else if (_cache.MainArea == null)
                input.HasScreenshot = TryCaptureAround(index, input.PhysicalX, input.PhysicalY);
        }

        // The crop around the click the review shows, and the whole main area for the draft to
        // cut its templates from. A failed encode costs the review a picture, never the recording.
        private void KeepPress(int index, RecordedInput input, RecordingScreenshot screenshot)
        {
            try
            {
                Rectangle around = AroundPress(input.PhysicalX, input.PhysicalY);
                RawImage crop = RawImageHelper.Crop(screenshot.Image, screenshot.Target.Bounds, around);
                if (!crop.IsEmpty)
                {
                    byte[] encodedCrop = _screenshotService.Encode(crop, ScreenshotFormatEnum.PNG, 100);
                    _cache.AddScreenshot(index, encodedCrop);
                    input.HasScreenshot = true;
                }

                byte[] encodedMainArea = _screenshotService.Encode(screenshot.Image, ScreenshotFormatEnum.PNG, 100);
                _cache.AddPress(new RecordedPress
                {
                    Index = index,
                    MainAreaBounds = screenshot.Target.Bounds,
                    Dpi = screenshot.Target.Dpi,
                    Screenshot = encodedMainArea,
                });
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
                RecordingProgressDto progress = GetProgress();
                return _broadcastService.SendAsync(BroadcastTypeEnum.RECORDING_PROGRESS, progress).AsTask();
            }
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

        // With no main area, the wizard's way: the screenshot around the click, taken once the
        // press is already on its way to the application.
        private bool TryCaptureAround(int index, int centreX, int centreY)
        {
            try
            {
                Rectangle around = AroundPress(centreX, centreY);
                Rectangle screen = _screenService.GetVirtualScreenBounds();
                Rectangle region = Rectangle.Intersect(around, screen);

                if (region.Width <= 0 || region.Height <= 0)
                    return false;

                byte[] screenshot = _screenshotService.Capture(region, ScreenshotFormatEnum.PNG, 100);
                _cache.AddScreenshot(index, screenshot);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // The capture size, centred on the press.
        private Rectangle AroundPress(int x, int y)
        {
            Size size = _cache.CaptureSize;
            return new Rectangle(x - size.Width / 2, y - size.Height / 2, size.Width, size.Height);
        }

        public void Dispose()
        {
            _inputRecordService.ActionRecorded -= OnActionRecorded;
            _queue?.Writer.TryComplete();
            _queue = null;
            _loopsCts?.Cancel();
        }


        // ================================================================
        // Private types
        // ================================================================

        // An input on its way from the hook to the drain, with the screenshot a press took at the
        // hook. Held by reference, so the drain can encode it after the cache has moved on.
        private sealed record PendingInput(RecordedInput Input, RecordingScreenshot? Screenshot);
    }
}
