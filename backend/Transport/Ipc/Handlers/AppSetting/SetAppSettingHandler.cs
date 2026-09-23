using Business.Services.AppSettingService;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
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
            await _appSettingService.SetAsync(request.Dto.Key, request.Dto.Value, ct);
            return ResultDto<bool>.Success(true);
        }
    }
}
