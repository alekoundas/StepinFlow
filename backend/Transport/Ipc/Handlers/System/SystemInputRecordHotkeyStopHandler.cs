using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordHotkeyStopHandler
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordHotkeyStopHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return ResultDto<bool>.Success(await _inputRecordService.StopRecordingHotkeyAsync());
        }
    }
}
