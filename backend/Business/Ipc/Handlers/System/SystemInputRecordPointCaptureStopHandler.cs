using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordPointCaptureStopHandler : IRequestHandler<SystemInputRecordPointCaptureStopCommand, ResultDto<bool>>
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordPointCaptureStopHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordPointCaptureStopCommand request, CancellationToken ct)
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
