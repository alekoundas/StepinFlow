using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Ipc.Commands.Flow;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowHandler : IRequestHandler<UpdateFlowCommand, UpdateFlowCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public UpdateFlowHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public async Task<UpdateFlowCommandResponse> Handle(UpdateFlowCommand request, CancellationToken ct)
        {
            Flow? flow = _mapper.Map<Flow>(request.dto);
            if (flow == null)
                return new UpdateFlowCommandResponse(null, false);

            var count = await _dataService.UpdateAsync(flow);
            if (count <= 0)
                return new UpdateFlowCommandResponse(null, false);
            else
                return new UpdateFlowCommandResponse(null, true); // Return null for now. TODO fix later
        }
    }
}
