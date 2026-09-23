using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Models.Dtos;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Transport.Ipc.Handlers;
using Transport.Ipc.Handlers.Ai;
using Transport.Ipc.Handlers.Execution;
using Transport.Ipc.Handlers.Lookup;
using Transport.Ipc.Protobuf;

namespace Transport.Ipc
{
    /// <summary>
    /// One request in, one response out.
    ///
    /// The switch is the protocol: every action the frontend can send is on this page, and the
    /// compiler checks each one - a duplicate action will not compile, and a handler whose
    /// signature changes breaks the arm that calls it. A route nobody wrote is a 404 on the first
    /// click in development, which is where a name mismatch belongs.
    /// </summary>
    public class IpcDispatcher
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,                 // JS -> .Net
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  // .Net -> JS
            ReferenceHandler = ReferenceHandler.IgnoreCycles,   // Ignore circular objects
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IServiceProvider _services;
        private readonly ILogger<IpcDispatcher> _logger;

        public IpcDispatcher(IServiceProvider services, ILogger<IpcDispatcher> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task<IpcResponse> HandleAsync(IpcRequest request, CancellationToken ct = default)
        {
            // Every request passes through here, so the argument is only built when Debug is on.
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[.NET Dispatcher]: Received {Action}", request.Action);

            try
            {
                object? responsePayload = request.Action switch
                {

                    // Flow
                    "Flow.create" => await Handler<CreateFlowHandler>().HandleAsync(Payload<FlowDto>(request), ct),
                    "Flow.update" => await Handler<UpdateFlowHandler>().HandleAsync(Payload<FlowDto>(request), ct),
                    "Flow.delete" => await Handler<DeleteFlowHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.get" => await Handler<GetFlowHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.getLazy" => await Handler<GetLazyFlowHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct),
                    "Flow.validate" => await Handler<ValidateFlowHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.getHealth" => await Handler<GetFlowHealthHandler>().HandleAsync(Payload<FlowHealthRequestDto>(request), ct),
                    "Flow.getCallers" => await Handler<GetFlowCallersHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.promoteToSubFlow" => await Handler<PromoteFlowToSubFlowHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.extractSubFlow" => await Handler<ExtractSubFlowHandler>().HandleAsync(Payload<ExtractSubFlowDto>(request), ct),
                    "Flow.getTreeNodes" => await Handler<GetFlowTreeNodeHandler>().HandleAsync(Payload<int>(request), ct),
                    "Flow.export" => await Handler<ExportFlowHandler>().HandleAsync(Payload<FlowExportRequestDto>(request), ct),
                    "Flow.import" => await Handler<ImportFlowHandler>().HandleAsync(Payload<FlowImportRequestDto>(request), ct),

                    // DiscordBot
                    "DiscordBot.create" => await Handler<CreateDiscordBotHandler>().HandleAsync(Payload<DiscordBotDto>(request), ct),
                    "DiscordBot.update" => await Handler<UpdateDiscordBotHandler>().HandleAsync(Payload<DiscordBotDto>(request), ct),
                    "DiscordBot.delete" => await Handler<DeleteDiscordBotHandler>().HandleAsync(Payload<int>(request), ct),
                    "DiscordBot.get" => await Handler<GetDiscordBotHandler>().HandleAsync(Payload<int>(request), ct),
                    "DiscordBot.getLazy" => await Handler<GetLazyDiscordBotHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct),
                    "DiscordBot.test" => await Handler<TestDiscordBotHandler>().HandleAsync(Payload<TestDiscordBotDto>(request), ct),

                    // FlowStep
                    "FlowStep.create" => await Handler<CreateFlowStepHandler>().HandleAsync(Payload<FlowStepDto>(request), ct),
                    "FlowStep.createMany" => await Handler<CreateFlowStepsHandler>().HandleAsync(Payload<FlowDraftDto>(request), ct),
                    "FlowStep.update" => await Handler<UpdateFlowStepHandler>().HandleAsync(Payload<FlowStepDto>(request), ct),
                    "FlowStep.delete" => await Handler<DeleteFlowStepHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowStep.get" => await Handler<GetFlowStepHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowStep.getLazy" => await Handler<GetLazyFlowStepHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct),
                    "FlowStep.getTreeNodes" => await Handler<GetFlowStepTreeNodeHandler>().HandleAsync(Payload<TreeNodeRequestDto>(request), ct),
                    "FlowStep.getTreeNodesRecursive" => await Handler<GetFlowStepTreeNodesRecursiveHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowStep.getDeleteImpact" => await Handler<GetFlowStepDeleteImpactHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowStep.getMovePreview" => await Handler<GetFlowStepMovePreviewHandler>().HandleAsync(Payload<FlowStepMoveDto>(request), ct),
                    "FlowStep.move" => await Handler<MoveFlowStepHandler>().HandleAsync(Payload<FlowStepMoveDto>(request), ct),

                    "FlowStep.testImageSearch" => await Handler<TestImageSearchHandler>().HandleAsync(Payload<FlowStepDto>(request), ct),
                    "FlowStep.testRunCommand" => await Handler<TestRunCommandHandler>().HandleAsync(Payload<FlowStepDto>(request), ct),
                    "FlowStep.testSearchText" => await Handler<TestSearchTextHandler>().HandleAsync(Payload<FlowStepDto>(request), ct),

                    // FlowArea
                    "FlowArea.create" => await Handler<CreateFlowAreaHandler>().HandleAsync(Payload<FlowAreaDto>(request), ct),
                    "FlowArea.update" => await Handler<UpdateFlowAreaHandler>().HandleAsync(Payload<FlowAreaDto>(request), ct),
                    "FlowArea.delete" => await Handler<DeleteFlowAreaHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowArea.get" => await Handler<GetFlowAreaHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowArea.getLazy" => await Handler<GetLazyFlowAreaHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct),
                    "FlowArea.getPreview" => await Handler<GetFlowAreaPreviewHandler>().HandleAsync(Payload<int>(request), ct),

                    // FlowPoint
                    "FlowPoint.create" => await Handler<CreateFlowPointHandler>().HandleAsync(Payload<FlowPointDto>(request), ct),
                    "FlowPoint.update" => await Handler<UpdateFlowPointHandler>().HandleAsync(Payload<FlowPointDto>(request), ct),
                    "FlowPoint.delete" => await Handler<DeleteFlowPointHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowPoint.get" => await Handler<GetFlowPointHandler>().HandleAsync(Payload<int>(request), ct),
                    "FlowPoint.getPreview" => await Handler<GetFlowPointPreviewHandler>().HandleAsync(Payload<int>(request), ct),

                    // FlowStepTemplate
                    "FlowStepTemplate.create" => await Handler<CreateFlowStepTemplateHandler>().HandleAsync(Payload<FlowStepTemplateDto>(request), ct),
                    "FlowStepTemplate.get" => await Handler<GetFlowStepTemplateHandler>().HandleAsync(Payload<int>(request), ct),

                    // Lookups
                    "Lookup.window" => await Handler<GetLookupWindowHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.monitor" => await Handler<GetLookupMonitorHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.flowStep" => await Handler<GetLookupFlowStepHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.flowPoint" => await Handler<GetLookupFlowPointHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.subFlow" => await Handler<GetLookupSubFlowHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.discordBot" => await Handler<GetLookupDiscordBotHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.failedStep" => await Handler<GetLookupFailedStepHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.testWindowMatch" => await Handler<TestWindowMatchHandler>().HandleAsync(Payload<WindowMatchTestRequestDto>(request), ct),
                    "Lookup.flowArea" => await Handler<GetLookupFlowAreaHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct),
                    "Lookup.commandPresets" => await GetLookupCommandPresetsHandler.HandleAsync(ct),
                    "Lookup.ocrLanguages" => await Handler<GetLookupOcrLanguagesHandler>().HandleAsync(ct),
                    "Lookup.aiModels" => await Handler<GetLookupAiModelsHandler>().HandleAsync(ct),
                    "Lookup.aiModelSuggestions" => await Handler<GetLookupAiModelSuggestionsHandler>().HandleAsync(ct),

                    // Recording
                    "Recording.start" => await Handler<StartRecordingHandler>().HandleAsync(ct),
                    "Recording.stop" => await Handler<StopRecordingHandler>().HandleAsync(ct),
                    "Recording.discard" => await Handler<DiscardRecordingHandler>().HandleAsync(ct),
                    "Recording.getScreenshot" => await Handler<GetRecordingScreenshotHandler>().HandleAsync(Payload<int>(request), ct),

                    // Settings
                    "Settings.getAll" => await Handler<GetAppSettingsHandler>().HandleAsync(ct),
                    "Settings.set" => await Handler<SetAppSettingHandler>().HandleAsync(Payload<SetAppSettingDto>(request), ct),

                    // System IO
                    "System.takeScreenshot" => await Handler<SystemTakeScreenshotHandler>().HandleAsync(Payload<ScreenshotRequestDto>(request), ct),
                    "System.captureForOverlay" => await Handler<SystemCaptureForOverlayHandler>().HandleAsync(ct),
                    "System.moveCursor" => await Handler<SystemMoveCursorHandler>().HandleAsync(Payload<ScreenPointDto>(request), ct),
                    "System.installOcrLanguage" => await Handler<SystemInstallOcrLanguageHandler>().HandleAsync(Payload<string>(request), ct),
                    "System.openWindowsLanguageSettings" => await Handler<SystemOpenWindowsLanguageSettingsHandler>().HandleAsync(ct),

                    "System.inputRecordAllStart" => await Handler<SystemInputRecordAllStartHandler>().HandleAsync(ct),
                    "System.inputRecordAllStop" => await Handler<SystemInputRecordAllStopHandler>().HandleAsync(ct),
                    "System.inputRecordOverlayStart" => await Handler<SystemInputRecordOverlayStartHandler>().HandleAsync(ct),
                    "System.inputRecordOverlayStop" => await Handler<SystemInputRecordOverlayStopHandler>().HandleAsync(ct),
                    "System.inputRecordPointCaptureStart" => await Handler<SystemInputRecordPointCaptureStartHandler>().HandleAsync(ct),
                    "System.inputRecordPointCaptureStop" => await Handler<SystemInputRecordPointCaptureStopHandler>().HandleAsync(ct),
                    "System.inputRecordHotkeyStart" => await Handler<SystemInputRecordHotkeyStartHandler>().HandleAsync(ct),
                    "System.inputRecordHotkeyStop" => await Handler<SystemInputRecordHotkeyStopHandler>().HandleAsync(ct),

                    // Execution
                    "Execution.start" => await Handler<StartExecutionHandler>().HandleAsync(Payload<ExecutionStartDto>(request), ct),
                    "Execution.stop" => await Handler<StopExecutionHandler>().HandleAsync(ct),
                    "Execution.pause" => await Handler<PauseExecutionHandler>().HandleAsync(ct),
                    "Execution.continue" => await Handler<ContinueExecutionHandler>().HandleAsync(ct),
                    "Execution.stepInto" => await Handler<StepIntoExecutionHandler>().HandleAsync(ct),
                    "Execution.stepOver" => await Handler<StepOverExecutionHandler>().HandleAsync(ct),
                    "Execution.setBreakpoints" => await Handler<SetExecutionBreakpointsHandler>().HandleAsync(Payload<List<int>>(request), ct),
                    "Execution.get" => await Handler<GetExecutionHandler>().HandleAsync(Payload<int>(request), ct),
                    "Execution.getList" => await Handler<GetExecutionListHandler>().HandleAsync(Payload<int>(request), ct),
                    "Execution.getState" => await Handler<GetExecutionStateHandler>().HandleAsync(ct),
                    "Execution.getFlowSummaries" => await Handler<GetFlowExecutionSummariesHandler>().HandleAsync(ct),
                    "Execution.getStepScreenshot" => await Handler<GetExecutionStepScreenshotHandler>().HandleAsync(Payload<int>(request), ct),

                    // Ai
                    "Ai.explainExecution" => await Handler<ExplainExecutionHandler>().HandleAsync(Payload<int>(request), ct),
                    "Ai.getStatus" => await Handler<GetAiStatusHandler>().HandleAsync(ct),
                    "Ai.getChatAvailability" => await Handler<GetAiChatAvailabilityHandler>().HandleAsync(ct),
                    "Ai.ask" => await Handler<AskAiHandler>().HandleAsync(Payload<AiChatRequestDto>(request), ct),
                    "Ai.downloadModel" => await Handler<DownloadAiModelHandler>().HandleAsync(Payload<string>(request), ct),
                    "Ai.getDownloadState" => await Handler<GetAiDownloadStateHandler>().HandleAsync(ct),
                    "Ai.clearDownloadState" => await Handler<ClearAiDownloadStateHandler>().HandleAsync(ct),
                    _ => throw new InvalidOperationException($"Unknown action: {request.Action}")
                };

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
                    : Encoding.UTF8.GetString(request.Payload, 0, Math.Min(request.Payload.Length, 512));

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


        // ================================================================
        // Private methods
        // ================================================================

        private T Handler<T>() where T : notnull
        {
            return _services.GetRequiredService<T>();
        }

        private static T Payload<T>(IpcRequest request)
        {
            return JsonSerializer.Deserialize<T>(request.Payload, _jsonOptions)!;
        }
    }
}
