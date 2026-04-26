using AutoMapper;
using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordStopHandler : IRequestHandler<SystemInputRecordStopCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordStopHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordStopCommand request, CancellationToken ct)
        {
            await _inputRecordService.StopRecordingAsync();

            return ResultDto<bool>.Success(true);
        }
    }
}
