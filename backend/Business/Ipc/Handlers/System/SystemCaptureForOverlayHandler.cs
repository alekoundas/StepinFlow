using AutoMapper;
using Business.Helpers;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class SystemCaptureForOverlayHandler : IRequestHandler<SystemCaptureForOverlayCommand, ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>>
    {
        private readonly IMapper _mapper;
        private readonly IScreenshotService _screenshotService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public SystemCaptureForOverlayHandler(IMapper mapper, IScreenshotService screenshotService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _screenshotService = screenshotService;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>> Handle(SystemCaptureForOverlayCommand request, CancellationToken ct)
        {
            List<ScreenshotMonitorResponseDto> response = new List<ScreenshotMonitorResponseDto>();

            IReadOnlyList<MonitorInfo> monitors = ScreenHelper.GetAllMonitors();
            foreach (MonitorInfo monitor in monitors)
            {

                byte[] screenshot = _screenshotService.CaptureMonitor(monitor.DeviceId, ScreenshotFormatEnum.JPEG, 100);

                response.Add(new ScreenshotMonitorResponseDto()
                {
                    Screenshot = screenshot,
                    LogicalX = monitor.Bounds.Left,
                    LogicalY = monitor.Bounds.Top,
                    PhysicalHeight= monitor.Bounds.Height,
                    PhysicalWidth= monitor.Bounds.Width
                }); 
            }

            return ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>.Success(response);
        }
    }
}
