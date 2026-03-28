using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateSubFlowHandler : IRequestHandler<CreateSubFlowCommand, ResultDto<int>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateSubFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<int>> Handle(CreateSubFlowCommand request, CancellationToken ct)
        {
            SubFlow subFlow = _mapper.Map<SubFlow>(request.dto);

            int count = await _dataService.AddAsync(subFlow);
            if (count <= 0)
                return ResultDto<int>.Failure("No changes made to the Database!");
            else
                return ResultDto<int>.Success(subFlow.Id);
        }
    }
}
