using Business.Services.AreaPointService;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetFlowPointPreviewHandler : IRequestHandler<GetFlowPointPreviewQuery, ResultDto<ScreenPointDto>>
    {
        private readonly IAreaPointResolver _areaPointResolver;

        public GetFlowPointPreviewHandler(IAreaPointResolver areaPointResolver)
        {
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ResultDto<ScreenPointDto>> Handle(GetFlowPointPreviewQuery request, CancellationToken ct)
        {
            PointResolution resolution = await _areaPointResolver.ResolvePointAsync(request.id, ct);

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
