using AutoMapper;
using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordOverlayStartHandler : IRequestHandler<SystemInputRecordOverlayStartCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordOverlayStartHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordOverlayStartCommand request, CancellationToken ct)
        {
            await _inputRecordService.StartRecordingOverlayAsync();

            return ResultDto<bool>.Success(true);
        }
    }
}
