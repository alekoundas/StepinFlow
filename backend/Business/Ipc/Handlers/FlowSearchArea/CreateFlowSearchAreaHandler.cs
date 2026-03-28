using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowSearchAreaHandler : IRequestHandler<CreateFlowSearchAreaCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowSearchAreaHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowSearchAreaCommand request, CancellationToken ct)
        {
            FlowSearchArea flowSearchArea = _mapper.Map<FlowSearchArea>(request.dto);

            int count = await _dataService.AddAsync(flowSearchArea);
            if (count <= 0)
                return ResultDto<int>.Failure("No changes made to the Database!");
            else
                return ResultDto<int>.Success(flowSearchArea.Id);
        }
    }
}
