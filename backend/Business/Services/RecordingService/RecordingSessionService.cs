using Business.Helpers;
using Business.Services.AppSettingService;
using Business.Services.InputService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Models.Business;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Channels;

namespace Business.Services.RecordingService
{
    /// <summary>
    /// Owns one recording at a time: the ordered actions, the screenshot taken for each click,
    /// and the live feed the recording page renders.
    ///
    /// The queue belongs to the session and is created with it, so a recording can never inherit
    /// events from before it started. Stopping completes the writer rather than cancelling the
    /// reader: a completed channel ends the read normally, where a cancelled one throws on every
    /// stop and leaves a single reader channel unusable for everything after it.
    ///
    /// Capture happens on the drain side, never in the event handler, because that handler runs
    /// on the hook thread and a screen capture costs tens of milliseconds. Pixels never go on the
    /// live feed either: the broadcast carries an index and the wizard asks for the image when it
    /// draws that action.
    /// </summary>
    public sealed class RecordingSessionService : IRecordingSessionService, IDisposable
    {
        /// <summary>
        /// Long enough for the hook to hand over what it already queued, so the last real action
        /// is not lost to the stop that follows it.
        /// </summary>
        private static readonly TimeSpan DrainDelay = TimeSpan.FromMilliseconds(150);

        private readonly IInputRecordService _inputRecordService;
        private readonly IScreenshotService _screenshotService;
        private readonly IAppSettingService _appSettingService;
        private readonly IIpcBroadcastService _broadcastService;

        private readonly List<RecordedInput> _events = new();
        private readonly ConcurrentDictionary<int, byte[]> _screenshots = new();
        private readonly Lock _eventsLock = new();

        private Channel<RecordedInput>? _queue;
        private Task? _drain;
        private Size _captureSize = new(400, 400);

        public RecordingSessionService(
            IInputRecordService inputRecordService,
            IScreenshotService screenshotService,
            IAppSettingService appSettingService,
            IIpcBroadcastService broadcastService)
        {
            _inputRecordService = inputRecordService;
            _screenshotService = screenshotService;
            _appSettingService = appSettingService;
            _broadcastService = broadcastService;
        }

        public bool IsRecording => _queue != null;


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<bool> StartAsync(CancellationToken ct = default)
        {
            if (IsRecording)
                return false;

            Clear();

            // Read once per session. Changing the capture size midway would leave the screenshots
            // inconsistent with each other for no benefit.
            _captureSize = new Size(
                await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureWidth, ct),
                await _appSettingService.GetAsync(AppSettingCatalog.RecordingCaptureHeight, ct));

            if (!await _inputRecordService.StartRecordingAllAsync())
                return false;

            Channel<RecordedInput> queue = Channel.CreateUnbounded<RecordedInput>(
                new UnboundedChannelOptions { SingleReader = true });

            _queue = queue;
            _drain = Task.Run(() => DrainAsync(queue), CancellationToken.None);
            _inputRecordService.ActionRecorded += OnActionRecorded;

            return true;
        }

        public async Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default)
        {
            Channel<RecordedInput>? queue = _queue;
            if (queue == null)
                return GetEvents();

            await _inputRecordService.StopRecordingAllAsync();
            _inputRecordService.ActionRecorded -= OnActionRecorded;

            // The hook stops publishing immediately but may still be inside a handler, so the
            // queue stays open long enough for those to land.
            await Task.Delay(DrainDelay, ct);

            _queue = null;
            queue.Writer.Complete();

            if (_drain != null)
            {
                await _drain;
                _drain = null;
            }

            return GetEvents();
        }

        public IReadOnlyList<RecordedInput> GetEvents()
        {
            lock (_eventsLock)
                return _events.ToList();
        }

        public byte[]? GetScreenshot(int index) =>
            _screenshots.TryGetValue(index, out byte[]? image) ? image : null;

        public void Clear()
        {
            lock (_eventsLock)
                _events.Clear();

            _screenshots.Clear();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // On the hook thread. Nothing here may block.
        private void OnActionRecorded(RecordedInput action) => _queue?.Writer.TryWrite(action);

        private async Task DrainAsync(Channel<RecordedInput> queue)
        {
            await foreach (RecordedInput action in queue.Reader.ReadAllAsync())
            {
                // One bad action must not end the drain. It is the only reader, so if it dies the
                // rest of the recording is silently lost.
                try
                {
                    await HandleAsync(action);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Recording] Dropped an action: {ex.Message}");
                }
            }
        }

        private async Task HandleAsync(RecordedInput action)
        {
            int index;
            lock (_eventsLock)
            {
                index = _events.Count;
                action.Index = index;
                _events.Add(action);
            }

            action.WindowTitle = TryGetForegroundWindowTitle();

            // The frame the user was looking at when they decided to click. Waiting for the
            // release would capture whatever the click had already changed.
            if (action.Type == RecordedInputTypeEnum.BUTTON_DOWN)
                action.HasScreenshot = TryCapture(index, action.PhysicalX, action.PhysicalY);

            await _broadcastService.SendAsync(BroadcastTypeEnum.RECORDING_EVENT, action);
        }

        private bool TryCapture(int index, int centreX, int centreY)
        {
            try
            {
                Rectangle region = Rectangle.Intersect(
                    new Rectangle(
                        centreX - _captureSize.Width / 2,
                        centreY - _captureSize.Height / 2,
                        _captureSize.Width,
                        _captureSize.Height),
                    ScreenHelper.GetVirtualScreenBounds());

                if (region.Width <= 0 || region.Height <= 0)
                    return false;

                _screenshots[index] = _screenshotService.Capture(region, ScreenshotFormatEnum.PNG, 100);
                return true;
            }
            catch
            {
                // A failed capture costs the wizard one screenshot, never the recording.
                return false;
            }
        }

        private static string? TryGetForegroundWindowTitle()
        {
            try
            {
                return AppWindowHelper.GetForegroundWindowTitle();
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _inputRecordService.ActionRecorded -= OnActionRecorded;
            _queue?.Writer.TryComplete();
            _queue = null;
        }
    }
}
