using AutoMapper;
using Core.Ports;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordOverlayStopHandler : IRequestHandler<SystemInputRecordOverlayStopCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordOverlayStopHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordOverlayStopCommand request, CancellationToken ct)
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
