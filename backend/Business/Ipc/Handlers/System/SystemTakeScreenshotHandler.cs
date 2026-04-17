using AutoMapper;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Business.Ipc.Handlers
{
    public class SystemTakeScreenshotHandler : IRequestHandler<SystemTakeScreenshotCommand, ResultDto<byte[]>>
    {
        private readonly IMapper _mapper;
        private readonly IScreenshotService _screenshotService;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public SystemTakeScreenshotHandler(IMapper mapper, IScreenshotService screenshotService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _screenshotService = screenshotService;
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<byte[]>> Handle(SystemTakeScreenshotCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            byte[] screenshot = [];

            if (request.dto.FlowSearchAreaId != null)
            {
                FlowSearchArea? flowSearchArea = await dbContext.FlowSearchAreas.FirstOrDefaultAsync(x => x.Id == request.dto.FlowSearchAreaId);
                if (flowSearchArea != null)
                    screenshot = _screenshotService.CaptureSearchArea(flowSearchArea);
                else
                    screenshot = _screenshotService.CaptureVirtualScreen();
            }
            else if (request.dto.IsVirtualScreen)
            {
                screenshot = _screenshotService.CaptureVirtualScreen();
            }
            else
            {
                Rectangle rect = new Rectangle(request.dto.LocationX, request.dto.LocationY, request.dto.Width, request.dto.Height);
                screenshot = _screenshotService.Capture(rect,ScreenshotFormatEnum.JPEG,100);
            }

            return ResultDto<byte[]>.Success(screenshot);
        }
    }
}
