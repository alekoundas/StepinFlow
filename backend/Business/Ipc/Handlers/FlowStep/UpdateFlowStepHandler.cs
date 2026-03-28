using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowStepHandler : IRequestHandler<UpdateFlowStepCommand, ResultDto<FlowStepDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public UpdateFlowStepHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<FlowStepDto>> Handle(UpdateFlowStepCommand request, CancellationToken ct)
        {
            FlowStep flowStep = _mapper.Map<FlowStep>(request.dto);

            int count = await _dataService.UpdateAsync(flowStep);
            if (count <= 0)
                return ResultDto<FlowStepDto>.Failure("No changes made to the Database!");

            FlowStepDto flowStepDto = _mapper.Map<FlowStepDto>(flowStep);
            return ResultDto<FlowStepDto>.Success(flowStepDto);
        }
    }
}
