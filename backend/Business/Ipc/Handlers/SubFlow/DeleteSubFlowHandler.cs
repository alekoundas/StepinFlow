using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class DeleteSubFlowHandler : IRequestHandler<DeleteSubFlowCommand, ResultDto<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public DeleteSubFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<bool>> Handle(DeleteSubFlowCommand request, CancellationToken ct)
        {
            int count = await _dataService.DeleteByIdAsync<SubFlow>(request.id);
            if (count <= 0)
                return ResultDto<bool>.Failure("No changes made to the Database!");
            else
                return ResultDto<bool>.Success(true);
        }
    }
}
