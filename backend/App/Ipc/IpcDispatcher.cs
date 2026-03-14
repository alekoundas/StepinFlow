
using Core.Models.Dtos;
using Core.Models.Ipc.Commands.Flow;
using Core.Models.Ipc.Commands.FlowSearchArea;
using Core.Models.Ipc.Commands.FlowStep;
using Core.Models.Ipc.Commands.FlowStepImage;
using Core.Models.Ipc.Commands.SubFlow;
using Core.Models.Ipc.Protobuf;
using MediatR;
using System.Text.Json;

namespace App.Ipc
{ 
    public class IpcDispatcher
    {
        private readonly IMediator _mediator;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public IpcDispatcher(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken ct = default)
        {
            try
            {
                object? responsePayload = request.Action switch
                {
                    // Flow
                    "Flow.create" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowCommand>(request.Payload, _jsonOptions) ?? new(new FlowCreateDto()), ct),
                    //"Flow.update" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowCommand>(request.Payload, _jsonOptions) ?? new(new FlowCreateDto()), ct),
                    //"Flow.delete" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowCommand>(request.Payload, _jsonOptions) ?? new(new FlowCreateDto()), ct),
                    "Flow.get" => await _mediator.Send(JsonSerializer.Deserialize<GetFlowQuery>(request.Payload, _jsonOptions) ?? new(-1), ct),

                    // FlowStep
                    "FlowStep.create" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowStepCommand>(request.Payload, _jsonOptions) ?? new(new FlowStepCreateDto()), ct),
                    "FlowStep.get" => await _mediator.Send(JsonSerializer.Deserialize<GetFlowStepQuery>(request.Payload, _jsonOptions) ?? new(-1), ct),

                    // FlowSearchArea
                    "FlowSearchArea.create" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowSearchAreaCommand>(request.Payload, _jsonOptions) ?? new(new FlowSearchAreaCreateDto()), ct),
                    "FlowSearchArea.get" => await _mediator.Send(JsonSerializer.Deserialize<GetFlowSearchAreaQuery>(request.Payload, _jsonOptions) ?? new(-1), ct),

                    // FlowStepImage 
                    "FlowStepImage.create" => await _mediator.Send(JsonSerializer.Deserialize<CreateFlowStepImageCommand>(request.Payload, _jsonOptions) ?? new(new FlowStepImageCreateDto()), ct),
                    "FlowStepImage.get" => await _mediator.Send(JsonSerializer.Deserialize<GetFlowStepImageQuery>(request.Payload, _jsonOptions) ?? new(-1), ct),

                    // SubFlow
                    "SubFlow.create" => await _mediator.Send(JsonSerializer.Deserialize<CreateSubFlowCommand>(request.Payload, _jsonOptions) ?? new(new SubFlowCreateDto()), ct),
                    "SubFlow.get" => await _mediator.Send(JsonSerializer.Deserialize<GetSubFlowQuery>(request.Payload, _jsonOptions) ?? new(-1), ct),


                    _ => throw new InvalidOperationException($"Unknown action: {request.Action}")
                };

                var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(responsePayload);

                return new IpcResponse
                {
                    Action = request.Action,
                    CorrelationId = request.CorrelationId,
                    Payload = payloadBytes,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return new IpcResponse
                {
                    Action = request.Action,
                    CorrelationId = request.CorrelationId,
                    Payload = Array.Empty<byte>(),
                    Error = ex.Message
                };
            }
        }
    }
}
