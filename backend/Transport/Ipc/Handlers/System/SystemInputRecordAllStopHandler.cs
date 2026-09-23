using AutoMapper;
using Core.Ports;
using Core.Models.Dtos;
using Transport.Messages;
using MediatR;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordAllStopHandler : IRequestHandler<SystemInputRecordAllStopCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordAllStopHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordAllStopCommand request, CancellationToken ct)
        {
            bool result = await _inputRecordService.StopRecordingAllAsync();

            if (result)
            {
                return ResultDto<bool>.Success(true);
            }

            return ResultDto<bool>.Failure("Recording should be started first dummy");
        }
    }
}
