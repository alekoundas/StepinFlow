using AutoMapper;
using Business.DataService.Services;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.FlowStepImage;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class CreateFlowStepImageHandler : IRequestHandler<CreateFlowStepImageCommand, CreateFlowStepImageCommandResponse>
    {
        private readonly IMapper _mapper;
        private readonly IDataService _dataService;

        public CreateFlowStepImageHandler(IMapper mapper, IDataService dataService)
        {
            _dataService = dataService;
            _mapper = mapper;
        }

        public Task<CreateFlowStepImageCommandResponse> Handle(CreateFlowStepImageCommand request, CancellationToken ct)
        {
            return Task.FromResult(new CreateFlowStepImageCommandResponse(1111, true));
        }
    }
}
