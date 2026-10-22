using Core.Ports;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemCaptureForOverlayHandler
    {
        private readonly IScreenshotService _screenshotService;
        private readonly IScreenService _screenService;

        public SystemCaptureForOverlayHandler(IScreenshotService screenshotService, IScreenService screenService)
        {
            _screenshotService = screenshotService;
            _screenService = screenService;
        }

        public async Task<ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>> HandleAsync(CancellationToken ct)
        {
            List<ScreenshotMonitorResponseDto> response = new List<ScreenshotMonitorResponseDto>();

            // Taken the way an execution takes its screenshots, and lossless: the overlay shows them,
            // and a template is cut from them, so it matches what a search will see. A fresh grab
            // after confirming would race the overlay fading out and catch its selection border.
            foreach (MonitorInfo monitor in _screenService.GetAllMonitors())
            {
                byte[] screenshot = _screenshotService.Capture(monitor.Bounds, ScreenshotFormatEnum.PNG, 100);

                response.Add(new ScreenshotMonitorResponseDto
                {
                    Screenshot = screenshot,
                    DeviceId = monitor.DeviceId,
                    X = monitor.Bounds.X,
                    Y = monitor.Bounds.Y,
                    Width = monitor.Bounds.Width,
                    Height = monitor.Bounds.Height,
                    Dpi = monitor.Dpi,
                });
            }

            return ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>.Success(response);
        }
    }
}
