using Business.AppSettings;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SetAppSettingHandler
    {
        private readonly IAppSettingService _appSettingService;

        public SetAppSettingHandler(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }

        public async Task<ResultDto<bool>> HandleAsync(SetAppSettingDto dto, CancellationToken ct)
        {
            await _appSettingService.SetAsync(dto.Key, dto.Value, ct);
            return ResultDto<bool>.Success(true);
        }
    }
}
