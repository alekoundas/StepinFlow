using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowHandler : IRequestHandler<CreateFlowCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<int>> Handle(CreateFlowCommand request, CancellationToken ct)
        {
            Flow flow = _mapper.Map<Flow>(request.dto);

            int count = await _dataService.AddAsync(flow);
            if (count <= 0)
                return ResultDto<int>.Failure("No changes made to the Database!");
            else
                return ResultDto<int>.Success(flow.Id);
        }
    }
}
