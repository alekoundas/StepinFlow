using AutoMapper;
using Business.AreaPoint;
using Core.Models.Business.AreaPointResolution;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Where a location as its form has it lands on screen now, worked out the way an execution
    /// does, so the form's Test goes where the flow will.
    /// </summary>
    public class ResolveFlowPointHandler
    {
        private readonly IMapper _mapper;
        private readonly IAreaPointResolver _areaPointResolver;

        public ResolveFlowPointHandler(IMapper mapper, IAreaPointResolver areaPointResolver)
        {
            _mapper = mapper;
            _areaPointResolver = areaPointResolver;
        }

        public async Task<ResultDto<ScreenPointDto>> HandleAsync(FlowPointDto dto, CancellationToken ct)
        {
            FlowPoint point = _mapper.Map<FlowPoint>(dto);
            PointResolution resolution = await _areaPointResolver.ResolvePointAsync(point, ct);

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
