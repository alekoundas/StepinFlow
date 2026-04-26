using AutoMapper;
using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
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
            await _inputRecordService.StopRecordingAllAsync();

            return ResultDto<bool>.Success(true);
        }
    }
}
