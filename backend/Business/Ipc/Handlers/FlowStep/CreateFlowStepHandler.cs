using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepHandler : IRequestHandler<CreateFlowStepCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowStepHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowStepCommand request, CancellationToken ct)
        {
            FlowStep flowStep = _mapper.Map<FlowStep>(request.dto);

            int count = await _dataService.AddAsync(flowStep);
            if (count <= 0)
                return ResultDto<int>.Failure("No changes made to the Database!");
            else
                return ResultDto<int>.Success(flowStep.Id);
        }
    }
}
