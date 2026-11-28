using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;
using Core.Models.Dtos.Repository;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Transport.Ipc.Handlers;
using Transport.Ipc.Handlers.Ai;
using Transport.Ipc.Handlers.Execution;
using Transport.Ipc.Handlers.Lookup;
using Transport.Ipc.Models;
using Transport.Ipc.Protobuf;

namespace Transport.Ipc
{
    /// <summary>
    /// One request in, one response out.
    ///
    /// Every action the frontend can send is on this page (The switch is the protocol).
    ///
    /// 2 lanes:InTurn and SideBySide.
    /// InTurn is is answered alone, in the order it came. 
    /// Only for: database, repository, execution, recording, mouse. 
    /// SideBySide changes nothing another request reads, so it is answered alongside the others. 
    /// 
    /// (Roslyn's language server queues its requests the same way.)
    /// </summary>
    public sealed class IpcDispatcher : IIpcDispatcher
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

        public IpcCall Prepare(IpcRequest request, CancellationToken ct)
        {
            // Every request passes through here, so the argument is only built when Debug is on.
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[.NET Dispatcher]: Received {Action}", request.Action);

            Route route = request.Action switch
            {

                // Flow
                "Flow.create" => InTurn(() => Handler<CreateFlowHandler>().HandleAsync(Payload<FlowDto>(request), ct)),
                "Flow.update" => InTurn(() => Handler<UpdateFlowHandler>().HandleAsync(Payload<FlowDto>(request), ct)),
                "Flow.delete" => InTurn(() => Handler<DeleteFlowHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.get" => SideBySide(() => Handler<GetFlowHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.getLazy" => SideBySide(() => Handler<GetLazyFlowHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct)),
                "Flow.validate" => SideBySide(() => Handler<ValidateFlowHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.getHealth" => SideBySide(() => Handler<GetFlowHealthHandler>().HandleAsync(Payload<FlowHealthRequestDto>(request), ct)),
                "Flow.getCallers" => SideBySide(() => Handler<GetFlowCallersHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.promoteToSubFlow" => InTurn(() => Handler<PromoteFlowToSubFlowHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.search" => SideBySide(() => Handler<SearchFlowsHandler>().HandleAsync(Payload<string>(request), ct)),
                "Flow.extractSubFlow" => InTurn(() => Handler<ExtractSubFlowHandler>().HandleAsync(Payload<ExtractSubFlowDto>(request), ct)),
                "Flow.getTreeNodes" => SideBySide(() => Handler<GetFlowTreeNodeHandler>().HandleAsync(Payload<int>(request), ct)),
                "Flow.getStepUses" => SideBySide(() => Handler<GetFlowStepUsesHandler>().HandleAsync(Payload<int>(request), ct)),

                // FlowScript
                "FlowScript.export" => SideBySide(() => Handler<ExportFlowScriptHandler>().HandleAsync(Payload<FlowScriptExportRequestDto>(request), ct)),
                "FlowScript.import" => InTurn(() => Handler<ImportFlowScriptHandler>().HandleAsync(Payload<FlowScriptImportRequestDto>(request), ct)),
                "FlowScript.preview" => SideBySide(() => Handler<PreviewFlowScriptHandler>().HandleAsync(Payload<FlowScriptImportRequestDto>(request), ct)),
                "FlowScript.getExportFolder" => SideBySide(() => Handler<GetFlowScriptExportFolderHandler>().HandleAsync(ct)),
                "FlowScript.render" => SideBySide(() => Handler<RenderFlowScriptHandler>().HandleAsync(Payload<int>(request), ct)),

                // Repository
                "Repository.getStatus" => SideBySide(() => Handler<GetRepositoryStatusHandler>().HandleAsync(ct)),
                "Repository.getChanges" => SideBySide(() => Handler<GetRepositoryChangesHandler>().HandleAsync(ct)),
                "Repository.getFlowChanges" => SideBySide(() => Handler<GetFlowRepositoryChangesHandler>().HandleAsync(Payload<int>(request), ct)),
                "Repository.commit" => InTurn(() => Handler<CommitRepositoryHandler>().HandleAsync(Payload<RepositoryCommitRequestDto>(request), ct)),
                "Repository.getFlowHistory" => SideBySide(() => Handler<GetFlowRepositoryHistoryHandler>().HandleAsync(Payload<int>(request), ct)),
                "Repository.getFlowCommit" => SideBySide(() => Handler<GetFlowRepositoryCommitHandler>().HandleAsync(Payload<RepositoryFlowCommitRequestDto>(request), ct)),
                "Repository.discard" => InTurn(() => Handler<DiscardFlowChangesHandler>().HandleAsync(Payload<int>(request), ct)),
                "Repository.restore" => InTurn(() => Handler<RestoreFlowVersionHandler>().HandleAsync(Payload<RepositoryFlowCommitRequestDto>(request), ct)),
                "Repository.getBranches" => SideBySide(() => Handler<GetRepositoryBranchesHandler>().HandleAsync(ct)),
                "Repository.createBranch" => InTurn(() => Handler<CreateRepositoryBranchHandler>().HandleAsync(Payload<string>(request), ct)),
                "Repository.switchBranch" => InTurn(() => Handler<SwitchRepositoryBranchHandler>().HandleAsync(Payload<string>(request), ct)),
                "Repository.connect" => SideBySide(() => Handler<ConnectRepositoryRemoteHandler>().HandleAsync(Payload<RepositoryConnectRequestDto>(request), ct)),
                "Repository.previewPull" => SideBySide(() => Handler<PreviewRepositoryPullHandler>().HandleAsync(ct)),
                "Repository.pull" => InTurn(() => Handler<PullRepositoryHandler>().HandleAsync(Payload<string>(request), ct)),
                "Repository.push" => SideBySide(() => Handler<PushRepositoryHandler>().HandleAsync(ct)),

                // DiscordBot
                "DiscordBot.create" => InTurn(() => Handler<CreateDiscordBotHandler>().HandleAsync(Payload<DiscordBotDto>(request), ct)),
                "DiscordBot.update" => InTurn(() => Handler<UpdateDiscordBotHandler>().HandleAsync(Payload<DiscordBotDto>(request), ct)),
                "DiscordBot.delete" => InTurn(() => Handler<DeleteDiscordBotHandler>().HandleAsync(Payload<int>(request), ct)),
                "DiscordBot.get" => SideBySide(() => Handler<GetDiscordBotHandler>().HandleAsync(Payload<int>(request), ct)),
                "DiscordBot.getLazy" => SideBySide(() => Handler<GetLazyDiscordBotHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct)),
                "DiscordBot.test" => SideBySide(() => Handler<TestDiscordBotHandler>().HandleAsync(Payload<TestDiscordBotDto>(request), ct)),

                // FlowStep
                "FlowStep.create" => InTurn(() => Handler<CreateFlowStepHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),
                "FlowStep.update" => InTurn(() => Handler<UpdateFlowStepHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),
                "FlowStep.delete" => InTurn(() => Handler<DeleteFlowStepHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowStep.get" => SideBySide(() => Handler<GetFlowStepHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowStep.getLazy" => SideBySide(() => Handler<GetLazyFlowStepHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct)),
                "FlowStep.getTreeNodes" => SideBySide(() => Handler<GetFlowStepTreeNodeHandler>().HandleAsync(Payload<TreeNodeRequestDto>(request), ct)),
                "FlowStep.getTreeNodesRecursive" => SideBySide(() => Handler<GetFlowStepTreeNodesRecursiveHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowStep.getDeleteImpact" => SideBySide(() => Handler<GetFlowStepDeleteImpactHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowStep.getMovePreview" => SideBySide(() => Handler<GetFlowStepMovePreviewHandler>().HandleAsync(Payload<FlowStepMoveDto>(request), ct)),
                "FlowStep.move" => InTurn(() => Handler<MoveFlowStepHandler>().HandleAsync(Payload<FlowStepMoveDto>(request), ct)),
                "FlowStep.paste" => InTurn(() => Handler<PasteFlowStepHandler>().HandleAsync(Payload<FlowStepPasteDto>(request), ct)),
                "FlowStep.setDisabled" => InTurn(() => Handler<SetFlowStepDisabledHandler>().HandleAsync(Payload<FlowStepDisabledDto>(request), ct)),
                "FlowStep.getScriptLine" => SideBySide(() => Handler<GetFlowStepScriptLineHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),

                "FlowStep.testImageSearch" => SideBySide(() => Handler<TestImageSearchHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),
                "FlowStep.testShellCommand" => SideBySide(() => Handler<TestShellCommandHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),
                "FlowStep.testSearchText" => SideBySide(() => Handler<TestSearchTextHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),
                "FlowStep.testCursor" => InTurn(() => Handler<TestCursorStepHandler>().HandleAsync(Payload<FlowStepDto>(request), ct)),

                // FlowArea
                "FlowArea.create" => InTurn(() => Handler<CreateFlowAreaHandler>().HandleAsync(Payload<FlowAreaDto>(request), ct)),
                "FlowArea.update" => InTurn(() => Handler<UpdateFlowAreaHandler>().HandleAsync(Payload<FlowAreaDto>(request), ct)),
                "FlowArea.delete" => InTurn(() => Handler<DeleteFlowAreaHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowArea.get" => SideBySide(() => Handler<GetFlowAreaHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowArea.getLazy" => SideBySide(() => Handler<GetLazyFlowAreaHandler>().HandleAsync(Payload<LazyRequestDto>(request), ct)),
                "FlowArea.getPreview" => SideBySide(() => Handler<GetFlowAreaPreviewHandler>().HandleAsync(Payload<int>(request), ct)),

                // FlowPoint
                "FlowPoint.create" => InTurn(() => Handler<CreateFlowPointHandler>().HandleAsync(Payload<FlowPointDto>(request), ct)),
                "FlowPoint.update" => InTurn(() => Handler<UpdateFlowPointHandler>().HandleAsync(Payload<FlowPointDto>(request), ct)),
                "FlowPoint.delete" => InTurn(() => Handler<DeleteFlowPointHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowPoint.get" => SideBySide(() => Handler<GetFlowPointHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowPoint.getPreview" => SideBySide(() => Handler<GetFlowPointPreviewHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowPoint.resolve" => SideBySide(() => Handler<ResolveFlowPointHandler>().HandleAsync(Payload<FlowPointDto>(request), ct)),

                // FlowInput
                "FlowInput.create" => InTurn(() => Handler<CreateFlowInputHandler>().HandleAsync(Payload<FlowInputDto>(request), ct)),
                "FlowInput.update" => InTurn(() => Handler<UpdateFlowInputHandler>().HandleAsync(Payload<FlowInputDto>(request), ct)),
                "FlowInput.delete" => InTurn(() => Handler<DeleteFlowInputHandler>().HandleAsync(Payload<int>(request), ct)),

                // FlowTemplate
                "FlowTemplate.getByFlow" => SideBySide(() => Handler<GetFlowTemplatesHandler>().HandleAsync(Payload<int>(request), ct)),
                "FlowTemplate.update" => InTurn(() => Handler<UpdateFlowTemplateHandler>().HandleAsync(Payload<FlowTemplateDto>(request), ct)),

                // Lookups
                "Lookup.window" => SideBySide(() => Handler<GetLookupWindowHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.monitor" => SideBySide(() => Handler<GetLookupMonitorHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.flowStep" => SideBySide(() => Handler<GetLookupFlowStepHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.flowPoint" => SideBySide(() => Handler<GetLookupFlowPointHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.subFlow" => SideBySide(() => Handler<GetLookupSubFlowHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.discordBot" => SideBySide(() => Handler<GetLookupDiscordBotHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.reportedStep" => SideBySide(() => Handler<GetLookupReportedStepHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.break" => SideBySide(() => Handler<GetLookupBreakHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.testWindowMatch" => SideBySide(() => Handler<TestWindowMatchHandler>().HandleAsync(Payload<WindowMatchTestRequestDto>(request), ct)),
                "Lookup.launchCommand" => SideBySide(() => Handler<GetLookupLaunchCommandHandler>().HandleAsync(Payload<int>(request), ct)),
                "Lookup.flowArea" => SideBySide(() => Handler<GetLookupFlowAreaHandler>().HandleAsync(Payload<LookupRequestDto>(request), ct)),
                "Lookup.shellCommandModes" => SideBySide(() => GetLookupShellCommandModesHandler.HandleAsync(ct)),
                "Lookup.ocrLanguages" => SideBySide(() => Handler<GetLookupOcrLanguagesHandler>().HandleAsync(ct)),
                "Lookup.aiModels" => SideBySide(() => Handler<GetLookupAiModelsHandler>().HandleAsync(ct)),
                "Lookup.aiModelSuggestions" => SideBySide(() => Handler<GetLookupAiModelSuggestionsHandler>().HandleAsync(ct)),

                // Recording
                "Recording.setup" => InTurn(() => Handler<SetupRecordingHandler>().HandleAsync(Payload<RecordingSetupDto>(request), ct)),
                "Recording.start" => InTurn(() => Handler<StartRecordingHandler>().HandleAsync(Payload<RecordingStartDto>(request), ct)),
                "Recording.pause" => InTurn(() => Handler<PauseRecordingHandler>().HandleAsync(ct)),
                "Recording.resume" => InTurn(() => Handler<ResumeRecordingHandler>().HandleAsync(ct)),
                "Recording.markStage" => InTurn(() => Handler<MarkRecordingStageHandler>().HandleAsync(ct)),
                "Recording.stop" => InTurn(() => Handler<StopRecordingHandler>().HandleAsync(ct)),
                "Recording.discard" => InTurn(() => Handler<DiscardRecordingHandler>().HandleAsync(Payload<RecordingDiscardDto>(request), ct)),
                "Recording.getResult" => SideBySide(() => Handler<GetRecordingResultHandler>().HandleAsync(ct)),
                "Recording.getScreen" => SideBySide(() => Handler<GetRecordingScreenHandler>().HandleAsync(Payload<int>(request), ct)),
                "Recording.review" => InTurn(() => Handler<ReviewRecordingHandler>().HandleAsync(Payload<ReviewedRecordingDto>(request), ct)),
                "Recording.getGesture" => SideBySide(() => Handler<GetRecordingGestureHandler>().HandleAsync(ct)),
                "Recording.decideGesture" => InTurn(() => Handler<DecideRecordingGestureHandler>().HandleAsync(Payload<RecordingGestureDecisionDto>(request), ct)),
                "Recording.testGestureRule" => SideBySide(() => Handler<TestRecordingGestureRuleHandler>().HandleAsync(Payload<RecordingGestureRuleDto>(request), ct)),
                "Recording.importDraft" => InTurn(() => Handler<ImportRecordingDraftHandler>().HandleAsync(Payload<int>(request), ct)),
                "Recording.insertDraft" => InTurn(() => Handler<InsertRecordingDraftHandler>().HandleAsync(Payload<RecordingDraftPositionDto>(request), ct)),
                "Recording.generateDraft" => InTurn(() => Handler<GenerateRecordingDraftHandler>().HandleAsync(Payload<int>(request), ct)),
                "Recording.reviseDraft" => InTurn(() => Handler<ReviseRecordingDraftHandler>().HandleAsync(Payload<RecordingDraftRevisionDto>(request), ct)),
                "Recording.dropProposedCheck" => InTurn(() => Handler<DropProposedCheckHandler>().HandleAsync(Payload<ProposedCheckDropDto>(request), ct)),
                "Recording.getGeneratedDraft" => SideBySide(() => Handler<GetGeneratedRecordingDraftHandler>().HandleAsync(ct)),
                "Recording.sendDraftRequest" => InTurn(() => Handler<SendRecordingDraftRequestHandler>().HandleAsync(ct)),
                "Recording.cancelDraftGeneration" => InTurn(() => Handler<CancelRecordingDraftGenerationHandler>().HandleAsync(ct)),

                // Settings
                "Settings.getAll" => SideBySide(() => Handler<GetAppSettingsHandler>().HandleAsync(ct)),
                "Settings.set" => InTurn(() => Handler<SetAppSettingHandler>().HandleAsync(Payload<SetAppSettingDto>(request), ct)),

                // System IO
                "System.takeScreenshot" => SideBySide(() => Handler<SystemTakeScreenshotHandler>().HandleAsync(Payload<ScreenshotRequestDto>(request), ct)),
                "System.captureForOverlay" => SideBySide(() => Handler<SystemCaptureForOverlayHandler>().HandleAsync(ct)),
                "System.cropScreenshots" => SideBySide(() => Handler<SystemCropScreenshotsHandler>().HandleAsync(Payload<ScreenshotCropRequestDto>(request), ct)),
                "System.moveCursor" => InTurn(() => Handler<SystemMoveCursorHandler>().HandleAsync(Payload<ScreenPointDto>(request), ct)),
                "System.downloadOcrLanguage" => SideBySide(() => Handler<SystemDownloadOcrLanguageHandler>().HandleAsync(Payload<string>(request), ct)),

                "System.inputRecordAllStart" => InTurn(() => Handler<SystemInputRecordAllStartHandler>().HandleAsync(ct)),
                "System.inputRecordAllStop" => InTurn(() => Handler<SystemInputRecordAllStopHandler>().HandleAsync(ct)),
                "System.inputRecordOverlayStart" => InTurn(() => Handler<SystemInputRecordOverlayStartHandler>().HandleAsync(ct)),
                "System.inputRecordOverlayStop" => InTurn(() => Handler<SystemInputRecordOverlayStopHandler>().HandleAsync(ct)),
                "System.inputRecordPointCaptureStart" => InTurn(() => Handler<SystemInputRecordPointCaptureStartHandler>().HandleAsync(ct)),
                "System.inputRecordPointCaptureStop" => InTurn(() => Handler<SystemInputRecordPointCaptureStopHandler>().HandleAsync(ct)),
                "System.inputRecordHotkeyStart" => InTurn(() => Handler<SystemInputRecordHotkeyStartHandler>().HandleAsync(ct)),
                "System.inputRecordHotkeyStop" => InTurn(() => Handler<SystemInputRecordHotkeyStopHandler>().HandleAsync(ct)),

                // Execution
                "Execution.start" => InTurn(() => Handler<StartExecutionHandler>().HandleAsync(Payload<ExecutionStartDto>(request), ct)),
                "Execution.stop" => InTurn(() => Handler<StopExecutionHandler>().HandleAsync(ct)),
                "Execution.pause" => InTurn(() => Handler<PauseExecutionHandler>().HandleAsync(ct)),
                "Execution.continue" => InTurn(() => Handler<ContinueExecutionHandler>().HandleAsync(ct)),
                "Execution.stepInto" => InTurn(() => Handler<StepIntoExecutionHandler>().HandleAsync(ct)),
                "Execution.stepOver" => InTurn(() => Handler<StepOverExecutionHandler>().HandleAsync(ct)),
                "Execution.setBreakpoints" => InTurn(() => Handler<SetExecutionBreakpointsHandler>().HandleAsync(Payload<List<int>>(request), ct)),

                "Execution.get" => SideBySide(() => Handler<GetExecutionHandler>().HandleAsync(Payload<int>(request), ct)),
                "Execution.getSteps" => SideBySide(() => Handler<GetExecutionStepsHandler>().HandleAsync(Payload<ExecutionStepRangeDto>(request), ct)),
                "Execution.getList" => SideBySide(() => Handler<GetExecutionListHandler>().HandleAsync(Payload<int>(request), ct)),
                "Execution.getState" => SideBySide(() => Handler<GetExecutionStateHandler>().HandleAsync(ct)),
                "Execution.getFlowSummaries" => SideBySide(() => Handler<GetFlowExecutionSummariesHandler>().HandleAsync(ct)),
                "Execution.getFeed" => SideBySide(() => Handler<GetExecutionFeedHandler>().HandleAsync(Payload<ExecutionFeedRequestDto>(request), ct)),
                "Execution.getStats" => SideBySide(() => Handler<GetExecutionStatsHandler>().HandleAsync(Payload<ExecutionStatsRequestDto>(request), ct)),
                "Execution.getStepScreenshot" => SideBySide(() => Handler<GetExecutionStepScreenshotHandler>().HandleAsync(Payload<int>(request), ct)),
                "Execution.delete" => InTurn(() => Handler<DeleteExecutionHandler>().HandleAsync(Payload<int>(request), ct)),
                "Execution.deleteForFlow" => InTurn(() => Handler<DeleteFlowExecutionsHandler>().HandleAsync(Payload<int>(request), ct)),

                // Cleanup
                "Cleanup.getStatus" => SideBySide(() => Handler<GetCleanupStatusHandler>().HandleAsync(ct)),
                "Cleanup.start" => InTurn(() => Handler<StartCleanupHandler>().HandleAsync(ct)),

                // Ai
                "Ai.explainExecution" => SideBySide(() => Handler<ExplainExecutionHandler>().HandleAsync(Payload<int>(request), ct)),
                "Ai.getStatus" => SideBySide(() => Handler<GetAiStatusHandler>().HandleAsync(ct)),
                "Ai.getLocalModelStatus" => SideBySide(() => Handler<GetAiLocalModelStatusHandler>().HandleAsync(ct)),
                "Ai.freeLocalModel" => SideBySide(() => Handler<FreeAiLocalModelHandler>().HandleAsync(ct)),
                "Ai.getChatAvailability" => SideBySide(() => Handler<GetAiChatAvailabilityHandler>().HandleAsync(ct)),
                "Ai.getChatImages" => SideBySide(() => Handler<GetAiChatImagesHandler>().HandleAsync(Payload<int>(request), ct)),
                "Ai.ask" => InTurn(() => Handler<AskAiHandler>().HandleAsync(Payload<AiChatRequestDto>(request), ct)),
                "Ai.stopAnswer" => InTurn(() => Handler<StopAiAnswerHandler>().HandleAsync(Payload<string>(request), ct)),
                "Ai.downloadModel" => InTurn(() => Handler<DownloadAiModelHandler>().HandleAsync(Payload<string>(request), ct)),
                "Ai.getDownloadState" => SideBySide(() => Handler<GetAiDownloadStateHandler>().HandleAsync(ct)),
                "Ai.clearDownloadState" => InTurn(() => Handler<ClearAiDownloadStateHandler>().HandleAsync(ct)),




                _ => InTurn<object?>(() => throw new InvalidOperationException($"Unknown action: {request.Action}"))
            };


            IpcCall ipcCall = new IpcCall 
            { 
                WaitsItsTurn = route.WaitsItsTurn, 
                Answer = () => AnswerAsync(request, route.Handle) 
            };

            return ipcCall;
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task<IpcResponse> AnswerAsync(IpcRequest request, Func<Task<object?>> handle)
        {
            try
            {
                // Call the actuall handler.
                object? responsePayload = await handle();
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

                Console.Error.WriteLine($"[.NET Dispatcher] '{request.Action}' failed: {ex.Message}{Environment.NewLine}  payload: {payloadPreview}");

                return new IpcResponse
                {
                    Action = request.Action,
                    CorrelationId = request.CorrelationId,
                    Payload = Array.Empty<byte>(),
                    Error = ex.Message
                };
            }
        }

        private static Route InTurn<T>(Func<Task<T>> handle)
        {
            return new Route(true, async () => await handle());
        }

        private static Route SideBySide<T>(Func<Task<T>> handle)
        {
            return new Route(false, async () => await handle());
        }

        private T Handler<T>() where T : notnull
        {
            return _services.GetRequiredService<T>();
        }

        private static T Payload<T>(IpcRequest request)
        {
            return JsonSerializer.Deserialize<T>(request.Payload, _jsonOptions)!;
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed record Route(bool WaitsItsTurn, Func<Task<object?>> Handle);
    }
}
