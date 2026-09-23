using Business.Services.Ai;
using Business.Services.Ai.AiModels;
using Business.Services.Ai.Providers;
using Core.Models.Dtos;


namespace Transport.Ipc.Handlers.Ai
{
    /// <summary>Reads a run and says what went wrong.</summary>
    public class ExplainExecutionHandler
    {
        private readonly IExecutionRunExplainService _runExplainService;

        public ExplainExecutionHandler(IExecutionRunExplainService runExplainService)
        {
            _runExplainService = runExplainService;
        }

        public async Task<ResultDto<AiAnswerDto>> HandleAsync(int executionId, CancellationToken ct)
        {
            AiAnswerDto answer = await _runExplainService.ExplainExecutionAsync(executionId, ct);
            return ResultDto<AiAnswerDto>.Success(answer);
        }
    }

    /// <summary>Whether a provider is set up, so the page can offer the button or explain why not.</summary>
    public class GetAiStatusHandler
    {
        private readonly IAiProviderService _providerService;

        public GetAiStatusHandler(IAiProviderService providerService)
        {
            _providerService = providerService;
        }

        public async Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            bool isConfigured = await _providerService.IsConfiguredAsync(ct);
            return ResultDto<bool>.Success(isConfigured);
        }
    }

    /// <summary>
    /// Starts a download and comes straight back. It runs for minutes, so how it is going arrives
    /// on the broadcast pipe rather than on this call.
    /// </summary>
    public class DownloadAiModelHandler
    {
        private readonly IAiModelService _modelService;

        public DownloadAiModelHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<bool>> HandleAsync(string model, CancellationToken ct)
        {
            bool isStarted = await _modelService.StartModelDownloadAsync(model, ct);

            if (!isStarted)
                return ResultDto<bool>.Failure("Downloading a model needs Ollama to be the chosen provider.");

            return ResultDto<bool>.Success(true);
        }
    }

    /// <summary>Whether the chat can be offered, and why not when it cannot.</summary>
    public class GetAiChatAvailabilityHandler
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public GetAiChatAvailabilityHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAvailabilityDto>> HandleAsync(CancellationToken ct)
        {
            return ResultDto<AiChatAvailabilityDto>.Success(await _flowQuestionService.GetAvailabilityAsync(ct));
        }
    }

    /// <summary>Answers a question about the flows, by letting the model query the database.</summary>
    public class AskAiHandler
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public AskAiHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAnswerDto>> HandleAsync(AiChatRequestDto dto, CancellationToken ct)
        {
            return ResultDto<AiChatAnswerDto>.Success(await _flowQuestionService.AskAsync(dto, ct));
        }
    }

    /// <summary>How the current or last download is going, for a page that has just opened./summary>
    public class GetAiDownloadStateHandler
    {
        private readonly IAiModelDownloadService _downloadService;

        public GetAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<AiModelDownloadEventDto?>> HandleAsync(CancellationToken ct)
        {
            return Task.FromResult(ResultDto<AiModelDownloadEventDto?>.Success(_downloadService.Current));
        }
    }

    /// <summary>Dismisses a finished download. A running one stays, because dismissing it would be a lie.</summary>
    public class ClearAiDownloadStateHandler
    {
        private readonly IAiModelDownloadService _downloadService;

        public ClearAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<bool>> HandleAsync(CancellationToken ct)
        {
            _downloadService.Clear();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
