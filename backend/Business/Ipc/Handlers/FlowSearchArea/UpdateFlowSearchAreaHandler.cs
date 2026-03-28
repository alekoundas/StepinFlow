using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowSearchAreaHandler : IRequestHandler<UpdateFlowSearchAreaCommand, ResultDto<FlowSearchAreaDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public UpdateFlowSearchAreaHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<FlowSearchAreaDto>> Handle(UpdateFlowSearchAreaCommand request, CancellationToken ct)
        {
            FlowSearchArea flowSearchArea = _mapper.Map<FlowSearchArea>(request.dto);

            int count = await _dataService.UpdateAsync(flowSearchArea);
            if (count <= 0)
                return ResultDto<FlowSearchAreaDto>.Failure("No changes made to the Database!");

            FlowSearchAreaDto flowSearchAreaDto = _mapper.Map<FlowSearchAreaDto>(flowSearchArea);
            return ResultDto<FlowSearchAreaDto>.Success(flowSearchAreaDto);
        }
    }
}
