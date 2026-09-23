using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class GetLookupMonitorHandler
    {
        private readonly IScreenService _screenService;

        public GetLookupMonitorHandler(IScreenService screenService)
        {
            _screenService = screenService;
        }

        public async Task<ResultDto<LookupResponseDto>> HandleAsync(LookupRequestDto dto, CancellationToken ct)
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
