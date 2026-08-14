
using Core.Models.Dtos;
using Core.Models.Ipc;
using Core.Models.Ipc.Protobuf;
using MediatR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Ipc
{
    public class IpcDispatcher
    {
        private readonly IMediator _mediator;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true, // JS -> .Net
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // .Net -> JS
            ReferenceHandler = ReferenceHandler.IgnoreCycles // Ignore circular objects

        };

        public IpcDispatcher(IMediator mediator)
        {
            _mediator = mediator;

            // Add Enum to string converter
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public async Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken ct = default)
        {
            Console.WriteLine("[.NET Dispatcher]: Recieved " + request.Action.ToString());
            try
            {
                object? responsePayload = request.Action switch
                {
                    // Flow
                    "Flow.create" => await _mediator.Send(new CreateFlowCommand(JsonSerializer.Deserialize<FlowDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.update" => await _mediator.Send(new UpdateFlowCommand(JsonSerializer.Deserialize<FlowDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.delete" => await _mediator.Send(new DeleteFlowCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.get" => await _mediator.Send(new GetFlowQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.getLazy" => await _mediator.Send(new GetLazyFlowQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.getTreeNodes" => await _mediator.Send(new GetFlowTreeNodeQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)!), ct),

                    // FlowStep
                    "FlowStep.create" => await _mediator.Send(new CreateFlowStepCommand(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.update" => await _mediator.Send(new UpdateFlowStepCommand(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.delete" => await _mediator.Send(new DeleteFlowStepCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.get" => await _mediator.Send(new GetFlowStepQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.getLazy" => await _mediator.Send(new GetLazyStepFlowQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.getTreeNodes" => await _mediator.Send(new GetFlowStepTreeNodeQuery(JsonSerializer.Deserialize<TreeNodeRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.getMovePreview" => await _mediator.Send(new GetFlowStepMovePreviewQuery(JsonSerializer.Deserialize<FlowStepMoveDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.move" => await _mediator.Send(new MoveFlowStepCommand(JsonSerializer.Deserialize<FlowStepMoveDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testImageSearch" => await _mediator.Send(new TestImageSearchQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testRunCommand" => await _mediator.Send(new TestRunCommandQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testTextSearch" => await _mediator.Send(new TestTextSearchQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),

                    // FlowSearchArea
                    "FlowSearchArea.create" => await _mediator.Send(new CreateFlowSearchAreaCommand(JsonSerializer.Deserialize<FlowSearchAreaDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowSearchArea.update" => await _mediator.Send(new UpdateFlowSearchAreaCommand(JsonSerializer.Deserialize<FlowSearchAreaDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowSearchArea.delete" => await _mediator.Send(new DeleteFlowSearchAreaCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowSearchArea.get" => await _mediator.Send(new GetFlowSearchAreaQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowSearchArea.getLazy" => await _mediator.Send(new GetLazyFlowSearchAreaQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowSearchArea.getPreview" => await _mediator.Send(new GetFlowSearchAreaPreviewQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // FlowLocation
                    "FlowLocation.create" => await _mediator.Send(new CreateFlowLocationCommand(JsonSerializer.Deserialize<FlowLocationDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowLocation.update" => await _mediator.Send(new UpdateFlowLocationCommand(JsonSerializer.Deserialize<FlowLocationDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowLocation.delete" => await _mediator.Send(new DeleteFlowLocationCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowLocation.get" => await _mediator.Send(new GetFlowLocationQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowLocation.getPreview" => await _mediator.Send(new GetFlowLocationPreviewQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // FlowStepImage
                    "FlowStepImage.create" => await _mediator.Send(new CreateFlowStepImageCommand(JsonSerializer.Deserialize<FlowStepImageDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStepImage.get" => await _mediator.Send(new GetFlowStepImageQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // SubFlow
                    "SubFlow.create" => await _mediator.Send(new CreateSubFlowCommand(JsonSerializer.Deserialize<SubFlowDto>(request.Payload, _jsonOptions)!), ct),
                    "SubFlow.update" => await _mediator.Send(new UpdateSubFlowCommand(JsonSerializer.Deserialize<SubFlowDto>(request.Payload, _jsonOptions)!), ct),
                    "SubFlow.delete" => await _mediator.Send(new DeleteSubFlowCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "SubFlow.get" => await _mediator.Send(new GetSubFlowQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // Lookups
                    "Lookup.window" => await _mediator.Send(new GetLookupWindowQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.monitor" => await _mediator.Send(new GetLookupMonitorQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowStep" => await _mediator.Send(new GetLookupFlowStepQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowLocation" => await _mediator.Send(new GetLookupFlowLocationQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowSearchArea" => await _mediator.Send(new GetLookupFlowSearchAreaQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.commandPresets" => await _mediator.Send(new GetLookupCommandPresetsQuery(), ct),

                    // System IO
                    "System.takeScreenshot" => await _mediator.Send(new SystemTakeScreenshotCommand(JsonSerializer.Deserialize<ScreenshotRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "System.captureForOverlay" => await _mediator.Send(new SystemCaptureForOverlayCommand(), ct),
                    "System.moveCursor" => await _mediator.Send(new SystemMoveCursorCommand(JsonSerializer.Deserialize<ScreenPointDto>(request.Payload, _jsonOptions)!), ct),

                    "System.inputRecordAllStart" => await _mediator.Send(new SystemInputRecordAllStartCommand(), ct),
                    "System.inputRecordAllStop" => await _mediator.Send(new SystemInputRecordAllStopCommand(), ct),
                    "System.inputRecordOverlayStart" => await _mediator.Send(new SystemInputRecordOverlayStartCommand(), ct),
                    "System.inputRecordOverlayStop" => await _mediator.Send(new SystemInputRecordOverlayStopCommand(), ct),
                    "System.inputRecordPointCaptureStart" => await _mediator.Send(new SystemInputRecordPointCaptureStartCommand(), ct),
                    "System.inputRecordPointCaptureStop" => await _mediator.Send(new SystemInputRecordPointCaptureStopCommand(), ct),

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


                byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(responsePayload, _jsonOptions);

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
                // Without the action and the payload a deserialization failure is just a stack
                // trace in System.Text.Json, with no way to tell which call sent what.
                string payloadPreview = request.Payload == null || request.Payload.Length == 0
                    ? "<empty>"
                    : System.Text.Encoding.UTF8.GetString(request.Payload, 0, Math.Min(request.Payload.Length, 512));

                Console.Error.WriteLine(
                    $"[.NET Dispatcher] '{request.Action}' failed: {ex.Message}{Environment.NewLine}  payload: {payloadPreview}");

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
