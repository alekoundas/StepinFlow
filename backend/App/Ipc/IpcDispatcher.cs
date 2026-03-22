
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
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
                    "Flow.create" => await _mediator.Send(new CreateFlowCommand(JsonSerializer.Deserialize<FlowCreateDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.update" => await _mediator.Send(new UpdateFlowCommand(JsonSerializer.Deserialize<FlowDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.delete" => await _mediator.Send(new DeleteFlowCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.get" => await _mediator.Send(new GetFlowQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.getLazy" => (await _mediator.Send(new GetLazyFlowQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct)).dto,
                    "Flow.getTreeNode" => (await _mediator.Send(new GetFlowTreeNodeQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)!), ct)),

                    // FlowStep
                    "FlowStep.create" => await _mediator.Send(new CreateFlowStepCommand(JsonSerializer.Deserialize<FlowStepCreateDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.get" => await _mediator.Send(new GetFlowStepQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.getTreeNode" => (await _mediator.Send(new GetFlowStepTreeNodeQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)!), ct)),

                    // FlowSearchArea
                    "FlowSearchArea.create" => await _mediator.Send(new CreateFlowSearchAreaCommand(JsonSerializer.Deserialize<FlowSearchAreaCreateDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowSearchArea.get" => await _mediator.Send(new GetFlowSearchAreaQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // FlowStepImage 
                    "FlowStepImage.create" => await _mediator.Send(new CreateFlowStepImageCommand(JsonSerializer.Deserialize<FlowStepImageCreateDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStepImage.get" => await _mediator.Send(new GetFlowStepImageQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // SubFlow
                    "SubFlow.create" => await _mediator.Send(new CreateSubFlowCommand(JsonSerializer.Deserialize<SubFlowCreateDto>(request.Payload, _jsonOptions)!), ct),
                    "SubFlow.get" => await _mediator.Send(new GetSubFlowQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),


                    _ => throw new InvalidOperationException($"Unknown action: {request.Action}")
                };

                //object? responsePayload = null;
                //switch (request.Action)
                //{
                //    // Flow
                //    case "Flow.create":
                //        //await _mediator.Send(JsonSerializer.Deserialize<CreateFlowCommand>(request.Payload, _jsonOptions) ?? new(new FlowCreateDto()), ct)
                //        var innerDto = JsonSerializer.Deserialize<FlowCreateDto>(request.Payload, _jsonOptions);
                //        Console.WriteLine($"Deserialized inner DTO: Name = {innerDto?.Name ?? "NULL"}, Order = {innerDto?.OrderNumber ?? -999}");

                //        var command = new CreateFlowCommand(innerDto ?? new FlowCreateDto());
                //        responsePayload = await _mediator.Send(command, ct);
                //        break;

                //}


                var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(responsePayload, _jsonOptions);

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
