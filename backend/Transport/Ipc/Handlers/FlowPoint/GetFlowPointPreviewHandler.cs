using Business.Services.AreaPointService;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class GetFlowPointPreviewHandler
    {
        private readonly IAreaPointResolver _areaPointResolver;

        public GetFlowPointPreviewHandler(IAreaPointResolver areaPointResolver)
        {
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ResultDto<ScreenPointDto>> HandleAsync(int id, CancellationToken ct)
        {
            PointResolution resolution = await _areaPointResolver.ResolvePointAsync(id, ct);

            if (!resolution.IsResolved)
                return ResultDto<ScreenPointDto>.Failure(resolution.Error!);

            return ResultDto<ScreenPointDto>.Success(
                new ScreenPointDto
                {
                    X = resolution.Point.X,
                    Y = resolution.Point.Y,
                });
        }
    }
}
