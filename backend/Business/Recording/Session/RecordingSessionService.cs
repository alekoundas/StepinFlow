using Business.AppSettings;
using Business.AreaPoint;
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
    /// It collects the raw inputs (presses, releases, scrolls, keys) and the screenshots taken around them.
    /// When the recording stops, it hands the inputs to RecordingActionBuilder, which folds them into actions. 
    /// It decides nothing about what the inputs mean.
    /// 
    /// While recording, the main area is captured a few times a second and the last few kept. A
    /// press takes the newest from before it, picked at the hook, so its screenshot shows the
    /// screen the tester decided on rather than what the press changed. Encoding happens on the
    /// drain side, never in the hook handler, which must return at once.
    ///
    /// The queue belongs to the session and is created with it, so a recording can never inherit
    /// events from before it started. Stopping completes the writer rather than cancelling the
    /// reader: a completed channel ends the read normally, where a cancelled one throws on every
    /// stop and leaves a single reader channel unusable for everything after it.
    ///
    /// Pixels never go on the live feed: the broadcast carries an index and the page asks for the
    /// image when it draws that action.
    /// </summary>
    public sealed class RecordingSessionService : IRecordingSessionService, IDisposable
    {
        // Long enough for the hook to hand over what it already queued, so the last real action
        // is not lost to the stop that follows it.
        private static readonly TimeSpan DRAIN_DELAY = TimeSpan.FromMilliseconds(150);

        // Often enough that the newest screenshot is from just before a press, and no more often:
        // a capture is real work beside the application being recorded.
        private static readonly TimeSpan CAPTURE_INTERVAL = TimeSpan.FromMilliseconds(250);

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
        private readonly RecordingSessionCache _cache = new RecordingSessionCache();

        private Channel<PendingInput>? _queue;
        private Task? _drain;
        private Task? _capture;
        private CancellationTokenSource? _captureCts;

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
            if (IsRecording)
                return ResultDto<bool>.Failure("A recording is already running.");

            // Without it the Stop click, and anything typed into StepinFlow, would be recorded.
            if (electronProcessId <= 0)
                return ResultDto<bool>.Failure("StepinFlow could not tell which windows are its own.");

            _cache.Clear();
            _filter.Reset();
            _filter.ElectronProcessId = electronProcessId;

            // Read once per session. Changing the capture size midway would leave the screenshots
            // inconsistent with each other for no benefit.
            int captureWidth = await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureWidth, ct);
            int captureHeight = await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureHeight, ct);
            _cache.CaptureSize = new Size(captureWidth, captureHeight);

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

            Channel<PendingInput> queue = Channel.CreateUnbounded<PendingInput>(new UnboundedChannelOptions { SingleReader = true });

            _queue = queue;
            _drain = Task.Run(() => DrainAsync(queue), CancellationToken.None);
            _inputRecordService.ActionRecorded += OnActionRecorded;

            if (mainArea != null)
            {
                CancellationTokenSource captureCts = new CancellationTokenSource();
                _captureCts = captureCts;
                _capture = Task.Run(() => CaptureLoopAsync(mainArea, captureCts.Token), CancellationToken.None);
            }

            return ResultDto<bool>.Success(true);
        }

        public void Pause()
        {
            if (IsRecording)
                _filter.IsPaused = true;
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
        }

        public async Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default)
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null)
                return _cache.GetInputs();

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

            await StopCapturingAsync();

            // The inputs and the screenshots per press stay for the review.
            _filter.Reset();
            _cache.ClearRecentScreenshots();
            _cache.MainArea = null;

            return _cache.GetInputs();
        }

        public IReadOnlyList<RecordedInput> GetEvents()
        {
            return _cache.GetInputs();
        }

        public byte[]? GetScreenshot(int index)
        {
            return _cache.GetScreenshot(index);
        }

        public RecordedPress? GetPress(int index)
        {
            return _cache.GetPress(index);
        }

        public void Clear()
        {
            _cache.Clear();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // On the hook's event thread, which every input after this one waits on. Nothing here may
        // block: a window lookup, and a screenshot picked by reference so the drain can encode it
        // after the cache has moved on.
        private void OnActionRecorded(RecordedInput input)
        {
            Channel<PendingInput>? queue = _queue;
            if (queue == null || !_filter.Keep(input))
                return;

            RecordingScreenshot? screenshot = null;
            if (input.Type == RecordedInputTypeEnum.BUTTON_DOWN)
                screenshot = _cache.GetRecentScreenshotBefore(input.CreatedOn);

            queue.Writer.TryWrite(new PendingInput(input, screenshot));
        }

        private async Task DrainAsync(Channel<PendingInput> queue)
        {
            await foreach (PendingInput pending in queue.Reader.ReadAllAsync())
            {
                // One bad action must not end the drain. It is the only reader, so if it dies the
                // rest of the recording is silently lost.
                try
                {
                    await HandleAsync(pending);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Recording] Dropped an action: {ex.Message}");
                }
            }
        }

        private async Task HandleAsync(PendingInput pending)
        {
            RecordedInput input = pending.Input;
            int index = _cache.AddInput(input);

            if (input.Type == RecordedInputTypeEnum.BUTTON_DOWN)
            {
                if (pending.Screenshot != null)
                    KeepPress(index, input, pending.Screenshot);
                else if (_cache.MainArea == null)
                    input.HasScreenshot = TryCaptureAround(index, input.PhysicalX, input.PhysicalY);
            }

            await _broadcastService.SendAsync(BroadcastTypeEnum.RECORDING_EVENT, input);
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

        private async Task StopCapturingAsync()
        {
            CancellationTokenSource? captureCts = _captureCts;
            if (captureCts == null)
                return;

            await captureCts.CancelAsync();
            if (_capture != null)
                await _capture;

            captureCts.Dispose();
            _captureCts = null;
            _capture = null;
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
            _captureCts?.Cancel();
        }


        // ================================================================
        // Private types
        // ================================================================

        // An input on its way from the hook to the drain, with the screenshot a press took at the
        // hook. Held by reference, so the drain can encode it after the cache has moved on.
        private sealed record PendingInput(RecordedInput Input, RecordingScreenshot? Screenshot);
    }
}
