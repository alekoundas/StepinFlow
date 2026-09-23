using AutoMapper;
using Core.Ports;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    public class SystemInputRecordOverlayStartHandler
    {
        private readonly IMapper _mapper;
        private readonly IInputRecordService _inputRecordService;

        public SystemInputRecordOverlayStartHandler(IMapper mapper, IInputRecordService inputRecordService)
        {
            _mapper = mapper;
            _inputRecordService= inputRecordService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool result = await _inputRecordService.StartRecordingOverlayAsync();

            if (result)
            {
                return ResultDto<bool>.Success(true);
            }

            return ResultDto<bool>.Failure("You cant run more than 1 recondings at the same time Broski");
        }
    }
}
