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

namespace Business.Services.RecordingService
{
    /// <summary>
    /// Owns one recording at a time: the ordered actions, the screenshot taken for each click,
    /// and the live feed the recording page renders.
    ///
    /// It reads InputRecordService's channel rather than being called from the hook, which is
    /// what keeps screen capture off the hook thread. A capture costs tens of milliseconds and a
    /// hook callback that slow is felt as input lag across the whole machine.
    ///
    /// Pixels never go on the live feed. A click screenshot is around 100KB and a session has
    /// dozens, so the broadcast carries an index and the wizard asks for the image when it draws
    /// that step.
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

        private readonly CancellationTokenSource _lifetimeCts = new();
        private readonly Lock _pumpLock = new();

        private Task? _pump;
        private volatile bool _isSessionOpen;
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

        public bool IsRecording => _isSessionOpen;


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

            EnsurePumpRunning();
            _isSessionOpen = true;

            return true;
        }

        public async Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default)
        {
            if (!_isSessionOpen)
                return GetEvents();

            await _inputRecordService.StopRecordingAllAsync();

            // The hook stops publishing immediately but the channel still holds what it queued,
            // so the session stays open long enough for the pump to hand those over.
            await Task.Delay(DrainDelay, ct);

            _isSessionOpen = false;

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

        /// <summary>
        /// Started once and left running.
        ///
        /// The channel has a single reader and outlives any one recording, so tearing the pump
        /// down between sessions would mean cancelling a blocked read: an exception thrown on
        /// every stop, for something that is not an error. Draining continuously also stops the
        /// channel filling with events from the overlay and point capture modes, which nothing
        /// else reads.
        /// </summary>
        private void EnsurePumpRunning()
        {
            lock (_pumpLock)
                _pump ??= Task.Run(() => PumpAsync(_lifetimeCts.Token), CancellationToken.None);
        }

        private async Task PumpAsync(CancellationToken ct)
        {
            await foreach (RecordedInput action in _inputRecordService.GetActions().WithCancellation(ct))
            {
                // Between sessions the pump still drains, so the overlay and point capture modes
                // cannot fill the channel behind us. Those events are simply not ours.
                if (!_isSessionOpen)
                    continue;

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
            _isSessionOpen = false;
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }
    }
}
