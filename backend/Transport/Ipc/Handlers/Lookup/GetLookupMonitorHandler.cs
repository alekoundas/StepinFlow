using Core.Ports;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
{
    public class GetLookupMonitorHandler : IRequestHandler<GetLookupMonitorQuery, ResultDto<LookupResponseDto>>
    {
        private readonly IScreenService _screenService;

        public GetLookupMonitorHandler(IScreenService screenService)
        {
            _screenService = screenService;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupMonitorQuery request, CancellationToken ct)
        {
            List<LookupItemDto> items = _screenService.GetAllMonitors().Select(monitor =>
                new LookupItemDto
                {
                    Value = monitor.DeviceId,
                    Label = monitor.FriendlyName,
                    Description = $"{monitor.Bounds.Width}×{monitor.Bounds.Height} @ ({monitor.Bounds.X}, {monitor.Bounds.Y})",
                    ExtraData = new
                    {
                        DeviceName = monitor.DeviceId,
                        IsPrimary = monitor.IsPrimary,
                        X = monitor.Bounds.X,
                        Y = monitor.Bounds.Y,
                        Width = monitor.Bounds.Width,
                        Height = monitor.Bounds.Height
                    }
                }).ToList();

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
