using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class UpdateSubFlowHandler : IRequestHandler<UpdateSubFlowCommand, ResultDto<SubFlowDto>>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public UpdateSubFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<ResultDto<SubFlowDto>> Handle(UpdateSubFlowCommand request, CancellationToken ct)
        {
            SubFlow subFlow = _mapper.Map<SubFlow>(request.dto);

            int count = await _dataService.UpdateAsync(subFlow);
            if (count <= 0)
                return ResultDto<SubFlowDto>.Failure("No changes made to the Database!");

            SubFlowDto subFlowDto = _mapper.Map<SubFlowDto>(subFlow);
            return ResultDto<SubFlowDto>.Success(subFlowDto);
        }
    }
}
