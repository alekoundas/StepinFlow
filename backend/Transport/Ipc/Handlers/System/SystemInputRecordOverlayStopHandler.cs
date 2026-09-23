using AutoMapper;
using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordOverlayStopHandler
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordOverlayStopHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool result = await _inputRecordService.StopRecordingOverlayAsync();

            if (result)
            {
                return ResultDto<bool>.Success(true);
            }

            return ResultDto<bool>.Failure("Recording should be started first dummy");
        }
    }
}
