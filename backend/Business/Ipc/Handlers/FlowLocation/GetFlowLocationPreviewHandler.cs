using Business.Services.FrameService;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetFlowLocationPreviewHandler : IRequestHandler<GetFlowLocationPreviewQuery, ResultDto<ScreenPointDto>>
    {
        private readonly IFrameResolver _frameResolver;

        public GetFlowLocationPreviewHandler(IFrameResolver frameResolver)
        {
            _frameResolver = frameResolver;
        }

        public async Task<ResultDto<ScreenPointDto>> Handle(GetFlowLocationPreviewQuery request, CancellationToken ct)
        {
            LocationResolution resolution = await _frameResolver.ResolveLocationAsync(request.id, ct);

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
