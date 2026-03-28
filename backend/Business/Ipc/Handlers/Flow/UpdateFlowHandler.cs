using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowHandler : IRequestHandler<UpdateFlowCommand, ResultDto<FlowDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public UpdateFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<FlowDto>> Handle(UpdateFlowCommand request, CancellationToken ct)
        {
            Flow flow = _mapper.Map<Flow>(request.dto);

            int count = await _dataService.UpdateAsync(flow);
            if (count <= 0)
                return ResultDto<FlowDto>.Failure("No changes made to the Database!");

            FlowDto flowDto = _mapper.Map<FlowDto>(flow);
            return ResultDto<FlowDto>.Success(flowDto);
        }
    }
}
