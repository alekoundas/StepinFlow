using Core.Models.Business;
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
            IReadOnlyList<MonitorInfo> monitors = _screenService.GetAllMonitors();
            MonitorInfo? primary = monitors.FirstOrDefault(x => x.IsPrimary);

            List<LookupItemDto> items = monitors.Select(monitor =>
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

            // First, and the default: the only choice that means the same monitor on another PC.
            items.Insert(0, new LookupItemDto
            {
                Value = string.Empty,
                Label = "Primary monitor",
                Description = primary == null ? "whichever monitor is primary" : $"currently {primary.FriendlyName}",
            });

            LookupResponseDto response = new LookupResponseDto { Data = items, TotalRecords = items.Count };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
