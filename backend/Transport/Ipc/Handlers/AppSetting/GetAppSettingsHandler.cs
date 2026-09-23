using Business.Services.AppSettingService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetAppSettingsHandler : IRequestHandler<GetAppSettingsQuery, ResultDto<IReadOnlyList<AppSettingDto>>>
    {
        private readonly IAppSettingService _appSettingService;

        public GetAppSettingsHandler(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }

        public async Task<ResultDto<IReadOnlyList<AppSettingDto>>> Handle(GetAppSettingsQuery request, CancellationToken ct) =>
            ResultDto<IReadOnlyList<AppSettingDto>>.Success(await _appSettingService.GetAllAsync(ct));
    }
}
