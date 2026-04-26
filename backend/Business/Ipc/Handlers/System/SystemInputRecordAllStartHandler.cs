using AutoMapper;
using Business.Services.InputService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class SystemInputRecordAllStartHandler : IRequestHandler<SystemInputRecordAllStartCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordAllStartHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> Handle(SystemInputRecordAllStartCommand request, CancellationToken ct)
        {
            await _inputRecordService.StartRecordingAllAsync();

            return ResultDto<bool>.Success(true);
        }
    }
}
