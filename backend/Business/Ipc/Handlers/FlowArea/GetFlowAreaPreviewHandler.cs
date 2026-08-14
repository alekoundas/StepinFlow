using Business.Services.FrameService;
using Business.Services.ScreenshotService;
using Core.Enums;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetFlowAreaPreviewHandler : IRequestHandler<GetFlowAreaPreviewQuery, ResultDto<FlowAreaPreviewDto>>
    {
        private readonly IFrameResolver _frameResolver;
        private readonly IScreenshotService _screenshotService;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowAreaPreviewHandler(
            IFrameResolver frameResolver,
            IScreenshotService screenshotService,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _frameResolver = frameResolver;
            _screenshotService = screenshotService;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowAreaPreviewDto>> Handle(GetFlowAreaPreviewQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            FlowArea? area = await dbContext.FlowAreas
                .AsNoTracking()
                .Include(x => x.ParentFlowArea)
                .FirstOrDefaultAsync(x => x.Id == request.id, ct);

            if (area == null)
                return ResultDto<FlowAreaPreviewDto>.Failure("Entity doesnt exist in the Database!");

            AreaResolution resolution = _frameResolver.ResolveArea(area);

            if (!resolution.IsResolved)
            {
                return ResultDto<FlowAreaPreviewDto>.Success(new FlowAreaPreviewDto
                {
                    IsResolved = false,
                    ErrorMessage = resolution.Error,
                });
            }

            return ResultDto<FlowAreaPreviewDto>.Success(new FlowAreaPreviewDto
            {
                IsResolved = true,
                LocationX = resolution.Bounds.X,
                LocationY = resolution.Bounds.Y,
                Width = resolution.Bounds.Width,
                Height = resolution.Bounds.Height,
                Screenshot = _screenshotService.CaptureResolvedArea(area, resolution.Bounds, ScreenshotFormatEnum.JPEG, 85),
            });
        }
    }
}
