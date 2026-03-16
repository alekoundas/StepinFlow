using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowHandler : IRequestHandler<CreateFlowCommand, CreateFlowCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<CreateFlowCommandResponse> Handle(CreateFlowCommand request, CancellationToken ct)
        {
            Flow? flow = _mapper.Map<Flow>(request.dto);
            if (flow == null)
                return new CreateFlowCommandResponse(-1, false);

            var count = await _dataService.AddAsync(flow);
            if (count <= 0)
                return new CreateFlowCommandResponse(-1, false);
            else
                return new CreateFlowCommandResponse(flow.Id, true);
        }
    }
}
