using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordPointCaptureStopHandler
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordPointCaptureStopHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool result = await _inputRecordService.StopRecordingPointCaptureAsync();

            if (result)
            {
                return ResultDto<bool>.Success(true);
            }

            return ResultDto<bool>.Failure("No point capture recording is running.");
        }
    }
}
