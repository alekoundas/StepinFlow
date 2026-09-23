using Business.Services.AppSettingService;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
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
