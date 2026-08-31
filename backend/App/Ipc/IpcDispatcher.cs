
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
                    "Flow.validate" => await _mediator.Send(new ValidateFlowQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.getHealth" => await _mediator.Send(new GetFlowHealthQuery(JsonSerializer.Deserialize<FlowHealthRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.getCallers" => await _mediator.Send(new GetFlowCallersQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.promoteToSubFlow" => await _mediator.Send(new PromoteFlowToSubFlowCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Flow.extractSubFlow" => await _mediator.Send(new ExtractSubFlowCommand(JsonSerializer.Deserialize<ExtractSubFlowDto>(request.Payload, _jsonOptions)!), ct),
                    "Flow.getTreeNodes" => await _mediator.Send(new GetFlowTreeNodeQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)!), ct),

                    // DiscordBot
                    "DiscordBot.create" => await _mediator.Send(new CreateDiscordBotCommand(JsonSerializer.Deserialize<DiscordBotDto>(request.Payload, _jsonOptions)!), ct),
                    "DiscordBot.update" => await _mediator.Send(new UpdateDiscordBotCommand(JsonSerializer.Deserialize<DiscordBotDto>(request.Payload, _jsonOptions)!), ct),
                    "DiscordBot.delete" => await _mediator.Send(new DeleteDiscordBotCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "DiscordBot.get" => await _mediator.Send(new GetDiscordBotQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "DiscordBot.getLazy" => await _mediator.Send(new GetLazyDiscordBotQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "DiscordBot.test" => await _mediator.Send(new TestDiscordBotCommand(JsonSerializer.Deserialize<TestDiscordBotDto>(request.Payload, _jsonOptions)!), ct),

                    // FlowStep
                    "FlowStep.create" => await _mediator.Send(new CreateFlowStepCommand(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.createMany" => await _mediator.Send(new CreateFlowStepsCommand(JsonSerializer.Deserialize<FlowDraftDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.update" => await _mediator.Send(new UpdateFlowStepCommand(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.delete" => await _mediator.Send(new DeleteFlowStepCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.get" => await _mediator.Send(new GetFlowStepQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.getLazy" => await _mediator.Send(new GetLazyStepFlowQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.getTreeNodes" => await _mediator.Send(new GetFlowStepTreeNodeQuery(JsonSerializer.Deserialize<TreeNodeRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.getTreeNodesRecursive" => await _mediator.Send(new GetFlowStepTreeNodesRecursiveQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowStep.getMovePreview" => await _mediator.Send(new GetFlowStepMovePreviewQuery(JsonSerializer.Deserialize<FlowStepMoveDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.move" => await _mediator.Send(new MoveFlowStepCommand(JsonSerializer.Deserialize<FlowStepMoveDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testImageSearch" => await _mediator.Send(new TestImageSearchQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testRunCommand" => await _mediator.Send(new TestRunCommandQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStep.testReadText" => await _mediator.Send(new TestReadTextQuery(JsonSerializer.Deserialize<FlowStepDto>(request.Payload, _jsonOptions)!), ct),

                    // FlowArea
                    "FlowArea.create" => await _mediator.Send(new CreateFlowAreaCommand(JsonSerializer.Deserialize<FlowAreaDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowArea.update" => await _mediator.Send(new UpdateFlowAreaCommand(JsonSerializer.Deserialize<FlowAreaDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowArea.delete" => await _mediator.Send(new DeleteFlowAreaCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowArea.get" => await _mediator.Send(new GetFlowAreaQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowArea.getLazy" => await _mediator.Send(new GetLazyFlowAreaQuery(JsonSerializer.Deserialize<LazyRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowArea.getPreview" => await _mediator.Send(new GetFlowAreaPreviewQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // FlowPoint
                    "FlowPoint.create" => await _mediator.Send(new CreateFlowPointCommand(JsonSerializer.Deserialize<FlowPointDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowPoint.update" => await _mediator.Send(new UpdateFlowPointCommand(JsonSerializer.Deserialize<FlowPointDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowPoint.delete" => await _mediator.Send(new DeleteFlowPointCommand(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowPoint.get" => await _mediator.Send(new GetFlowPointQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "FlowPoint.getPreview" => await _mediator.Send(new GetFlowPointPreviewQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // FlowStepImage
                    "FlowStepImage.create" => await _mediator.Send(new CreateFlowStepImageCommand(JsonSerializer.Deserialize<FlowStepImageDto>(request.Payload, _jsonOptions)!), ct),
                    "FlowStepImage.get" => await _mediator.Send(new GetFlowStepImageQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // Lookups
                    "Lookup.window" => await _mediator.Send(new GetLookupWindowQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.monitor" => await _mediator.Send(new GetLookupMonitorQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowStep" => await _mediator.Send(new GetLookupFlowStepQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowPoint" => await _mediator.Send(new GetLookupFlowPointQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.subFlow" => await _mediator.Send(new GetLookupSubFlowQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.discordBot" => await _mediator.Send(new GetLookupDiscordBotQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.failedStep" => await _mediator.Send(new GetLookupFailedStepQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.testWindowMatch" => await _mediator.Send(new TestWindowMatchQuery(JsonSerializer.Deserialize<WindowMatchTestRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.flowArea" => await _mediator.Send(new GetLookupFlowAreaQuery(JsonSerializer.Deserialize<LookupRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "Lookup.commandPresets" => await _mediator.Send(new GetLookupCommandPresetsQuery(), ct),
                    "Lookup.ocrLanguages" => await _mediator.Send(new GetLookupOcrLanguagesQuery(), ct),

                    // Recording
                    "Recording.start" => await _mediator.Send(new StartRecordingCommand(), ct),
                    "Recording.stop" => await _mediator.Send(new StopRecordingCommand(), ct),
                    "Recording.discard" => await _mediator.Send(new DiscardRecordingCommand(), ct),
                    "Recording.getScreenshot" => await _mediator.Send(new GetRecordingScreenshotQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),

                    // Settings
                    "Settings.getAll" => await _mediator.Send(new GetAppSettingsQuery(), ct),
                    "Settings.set" => await _mediator.Send(new SetAppSettingCommand(JsonSerializer.Deserialize<SetAppSettingDto>(request.Payload, _jsonOptions)!), ct),

                    // System IO
                    "System.takeScreenshot" => await _mediator.Send(new SystemTakeScreenshotCommand(JsonSerializer.Deserialize<ScreenshotRequestDto>(request.Payload, _jsonOptions)!), ct),
                    "System.captureForOverlay" => await _mediator.Send(new SystemCaptureForOverlayCommand(), ct),
                    "System.moveCursor" => await _mediator.Send(new SystemMoveCursorCommand(JsonSerializer.Deserialize<ScreenPointDto>(request.Payload, _jsonOptions)!), ct),
                    "System.installOcrLanguage" => await _mediator.Send(new SystemInstallOcrLanguageCommand(JsonSerializer.Deserialize<string>(request.Payload, _jsonOptions)!), ct),
                    "System.openWindowsLanguageSettings" => await _mediator.Send(new SystemOpenWindowsLanguageSettingsCommand(), ct),

                    "System.inputRecordAllStart" => await _mediator.Send(new SystemInputRecordAllStartCommand(), ct),
                    "System.inputRecordAllStop" => await _mediator.Send(new SystemInputRecordAllStopCommand(), ct),
                    "System.inputRecordOverlayStart" => await _mediator.Send(new SystemInputRecordOverlayStartCommand(), ct),
                    "System.inputRecordOverlayStop" => await _mediator.Send(new SystemInputRecordOverlayStopCommand(), ct),
                    "System.inputRecordPointCaptureStart" => await _mediator.Send(new SystemInputRecordPointCaptureStartCommand(), ct),
                    "System.inputRecordPointCaptureStop" => await _mediator.Send(new SystemInputRecordPointCaptureStopCommand(), ct),
                    "System.inputRecordHotkeyStart" => await _mediator.Send(new SystemInputRecordHotkeyStartCommand(), ct),
                    "System.inputRecordHotkeyStop" => await _mediator.Send(new SystemInputRecordHotkeyStopCommand(), ct),

                    // Execution
                    "Execution.start" => await _mediator.Send(new StartExecutionCommand(JsonSerializer.Deserialize<ExecutionStartDto>(request.Payload, _jsonOptions)!), ct),
                    "Execution.stop" => await _mediator.Send(new StopExecutionCommand(), ct),
                    "Execution.pause" => await _mediator.Send(new PauseExecutionCommand(), ct),
                    "Execution.continue" => await _mediator.Send(new ContinueExecutionCommand(), ct),
                    "Execution.stepInto" => await _mediator.Send(new StepIntoExecutionCommand(), ct),
                    "Execution.stepOver" => await _mediator.Send(new StepOverExecutionCommand(), ct),
                    "Execution.setBreakpoints" => await _mediator.Send(new SetExecutionBreakpointsCommand(JsonSerializer.Deserialize<List<int>>(request.Payload, _jsonOptions)!), ct),
                    "Execution.get" => await _mediator.Send(new GetExecutionQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Execution.getList" => await _mediator.Send(new GetExecutionListQuery(JsonSerializer.Deserialize<int>(request.Payload, _jsonOptions)), ct),
                    "Execution.getState" => await _mediator.Send(new GetExecutionStateQuery(), ct),

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
