using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordPointCaptureStartHandler : IRequestHandler<SystemInputRecordPointCaptureStartCommand, ResultDto<bool>>
    {
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordPointCaptureStartHandler(IInputRecordService inputRecordService)
        {
            _inputRecordService = inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordPointCaptureStartCommand request, CancellationToken ct)
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
