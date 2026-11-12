using Business.AreaPoint;
using Business.Recording.Models.Session;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Ports;

namespace Business.Recording.Session
{
    /// <summary>
    /// The main area captured a few times a second while recording, the last few kept, so a press
    /// takes the newest from before it and its screenshot shows the screen the tester decided on.
    ///
    /// A website's tab is found by its address once, as the recording begins, and its window
    /// followed after: a page that goes on to a sign-in on another site is still the one being
    /// recorded. Found again by the address only once that window is closed or minimised.
    /// </summary>
    public sealed class RecordingScreenCapture
    {
        // Often enough that the newest screenshot is from just before a press, and no more often:
        // a capture is real work beside the application being recorded.
        private static readonly TimeSpan INTERVAL = TimeSpan.FromMilliseconds(250);

        private readonly IAreaPointResolver _areaPointResolver;
        private readonly IWindowService _windowService;
        private readonly IScreenshotService _screenshotService;
        private readonly RecordingSessionCache _cache;
        private readonly RecordingInputFilter _filter;
        private readonly TimeProvider _timeProvider;

        // The browser window a website's tab was found in when the recording began.
        private WindowHandle _tabWindow = WindowHandle.None;

        public RecordingScreenCapture(
            IAreaPointResolver areaPointResolver,
            IWindowService windowService,
            IScreenshotService screenshotService,
            RecordingSessionCache cache,
            RecordingInputFilter filter,
            TimeProvider timeProvider)
        {
            _areaPointResolver = areaPointResolver;
            _windowService = windowService;
            _screenshotService = screenshotService;
            _cache = cache;
            _filter = filter;
            _timeProvider = timeProvider;
        }


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>A new recording finds its tab again.</summary>
        public void Reset()
        {
            _tabWindow = WindowHandle.None;
        }

        /// <summary>One screenshot of the main area, kept among the recent ones. Null when it worked, else why not.</summary>
        public string? Capture(FlowArea mainArea)
        {
            AreaResolution area = Resolve(mainArea);
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

        /// <summary>
        /// Captures until cancelled. A main area minimised or closed for a moment keeps the
        /// screenshots from before it, rather than ending the recording.
        /// </summary>
        public async Task LoopAsync(FlowArea mainArea, CancellationToken ct)
        {
            using PeriodicTimer timer = new PeriodicTimer(INTERVAL, _timeProvider);

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    try
                    {
                        Capture(mainArea);
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


        // ================================================================
        // Private methods
        // ================================================================

        private AreaResolution Resolve(FlowArea mainArea)
        {
            if (mainArea.Type != FlowAreaTypeEnum.BROWSER_TAB)
                return _areaPointResolver.ResolveArea(mainArea);

            if (_tabWindow.IsValid)
            {
                AreaResolution followed = _areaPointResolver.ResolveTabPage(mainArea, _tabWindow);
                if (followed.IsResolved)
                    return followed;
            }

            AreaResolution found = _areaPointResolver.ResolveArea(mainArea);
            if (found.IsResolved)
                _tabWindow = found.Window;

            return found;
        }
    }
}
