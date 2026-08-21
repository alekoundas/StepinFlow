using Business.Services.AppSettingService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SetAppSettingHandler : IRequestHandler<SetAppSettingCommand, ResultDto<bool>>
    {
        private readonly IAppSettingService _appSettingService;

        public SetAppSettingHandler(IAppSettingService appSettingService)
        {
            _appSettingService = appSettingService;
        }

        public async Task<ResultDto<bool>> Handle(SetAppSettingCommand request, CancellationToken ct)
        {
            await _appSettingService.SetAsync(request.dto.Key, request.dto.Value, ct);
            return ResultDto<bool>.Success(true);
        }
    }
}
