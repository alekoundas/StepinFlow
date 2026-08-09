using Business.Helpers;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemCaptureForOverlayHandler : IRequestHandler<SystemCaptureForOverlayCommand, ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>>
    {
        private readonly IScreenshotService _screenshotService;

        public SystemCaptureForOverlayHandler(IScreenshotService screenshotService)
        {
            _screenshotService = screenshotService;
        }

        public async Task<ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>> Handle(SystemCaptureForOverlayCommand request, CancellationToken ct)
        {
            List<ScreenshotMonitorResponseDto> response = new List<ScreenshotMonitorResponseDto>();

            foreach (MonitorInfo monitor in ScreenHelper.GetAllMonitors())
            {
                byte[] screenshot = _screenshotService.CaptureMonitor(monitor.DeviceId, ScreenshotFormatEnum.JPEG, 100);

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
