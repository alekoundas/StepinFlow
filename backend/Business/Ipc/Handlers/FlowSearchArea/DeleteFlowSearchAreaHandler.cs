using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class DeleteFlowSearchAreaHandler : IRequestHandler<DeleteFlowSearchAreaCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public DeleteFlowSearchAreaHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<bool>> Handle(DeleteFlowSearchAreaCommand request, CancellationToken ct)
        {
            int count = await _dataService.DeleteByIdAsync<FlowSearchArea>(request.id);
            if (count <= 0)
                return ResultDto<bool>.Failure("No changes made to the Database!");
            else
                return ResultDto<bool>.Success(true);
        }
    }
}
