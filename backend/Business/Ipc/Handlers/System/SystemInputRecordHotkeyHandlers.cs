using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordHotkeyStartHandler : IRequestHandler<SystemInputRecordHotkeyStartCommand, ResultDto<bool>>
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordHotkeyStartHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordHotkeyStartCommand request, CancellationToken ct) =>
            ResultDto<bool>.Success(await _inputRecordService.StartRecordingHotkeyAsync());
    }

    public class SystemInputRecordHotkeyStopHandler : IRequestHandler<SystemInputRecordHotkeyStopCommand, ResultDto<bool>>
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordHotkeyStopHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordHotkeyStopCommand request, CancellationToken ct) =>
            ResultDto<bool>.Success(await _inputRecordService.StopRecordingHotkeyAsync());
    }
}
