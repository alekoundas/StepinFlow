using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordPointCaptureStartHandler
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordPointCaptureStartHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool result = await _inputRecordService.StartRecordingPointCaptureAsync();

            if (result)
            {
                return ResultDto<bool>.Success(true);
            }

            return ResultDto<bool>.Failure("You cant run more than 1 recondings at the same time Broski");
        }
    }
}
