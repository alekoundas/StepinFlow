using Business.Services.AppSettingService;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class GetAppSettingsHandler
    {
        private readonly IAppSettingService _appSettingService;

        public GetAppSettingsHandler(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }

        public async Task<ResultDto<IReadOnlyList<AppSettingDto>>> HandleAsync(CancellationToken ct) =>
            ResultDto<IReadOnlyList<AppSettingDto>>.Success(await _appSettingService.GetAllAsync(ct));
    }
}
