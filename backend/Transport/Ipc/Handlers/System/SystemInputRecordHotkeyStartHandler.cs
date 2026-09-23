using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordHotkeyStartHandler
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordHotkeyStartHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            return ResultDto<bool>.Success(await _inputRecordService.StartRecordingHotkeyAsync());
        }
    }
}
