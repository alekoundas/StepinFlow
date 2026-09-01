using Business.Services.AiService;
using Core.Models.Dtos;
using Core.Models.Ipc;

using MediatR;

namespace Business.Ipc.Handlers.Ai
{
    /// <summary>Reads a run and says what went wrong.</summary>
    public class ExplainExecutionHandler : IRequestHandler<ExplainExecutionQuery, ResultDto<AiAnswerDto>>
    {
        private readonly IAiService _aiService;

        public ExplainExecutionHandler(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ResultDto<AiAnswerDto>> Handle(ExplainExecutionQuery request, CancellationToken ct)
        {
            AiAnswerDto answer = await _aiService.ExplainExecutionAsync(request.executionId, ct);
            return ResultDto<AiAnswerDto>.Success(answer);
        }
    }

    /// <summary>Whether a provider is set up, so the page can offer the button or explain why not.</summary>
    public class GetAiStatusHandler : IRequestHandler<GetAiStatusQuery, ResultDto<bool>>
    {
        private readonly IAiService _aiService;

        public GetAiStatusHandler(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ResultDto<bool>> Handle(GetAiStatusQuery request, CancellationToken ct)
        {
            bool isConfigured = await _aiService.IsConfiguredAsync(ct);
            return ResultDto<bool>.Success(isConfigured);
        }
    }

    /// <summary>What the chosen provider offers, so the model setting is a list and not a guess.</summary>
    public class GetAiModelsHandler : IRequestHandler<GetAiModelsQuery, ResultDto<AiModelsDto>>
    {
        private readonly IAiService _aiService;

        public GetAiModelsHandler(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ResultDto<AiModelsDto>> Handle(GetAiModelsQuery request, CancellationToken ct)
        {
            AiModelsDto models = await _aiService.GetModelsAsync(ct);
            return ResultDto<AiModelsDto>.Success(models);
        }
    }

    /// <summary>Local models worth offering, with the ones already pulled marked.</summary>
    public class GetAiModelSuggestionsHandler
        : IRequestHandler<GetAiModelSuggestionsQuery, ResultDto<IReadOnlyList<AiModelSuggestionDto>>>
    {
        private readonly IAiService _aiService;

        public GetAiModelSuggestionsHandler(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ResultDto<IReadOnlyList<AiModelSuggestionDto>>> Handle(GetAiModelSuggestionsQuery request, CancellationToken ct)
        {
            IReadOnlyList<AiModelSuggestionDto> suggestions = await _aiService.GetModelSuggestionsAsync(ct);
            return ResultDto<IReadOnlyList<AiModelSuggestionDto>>.Success(suggestions);
        }
    }

    /// <summary>
    /// Starts a download and comes straight back. It runs for minutes, so how it is going arrives
    /// on the broadcast pipe rather than on this call.
    /// </summary>
    public class PullAiModelHandler : IRequestHandler<PullAiModelCommand, ResultDto<bool>>
    {
        private readonly IAiService _aiService;

        public PullAiModelHandler(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ResultDto<bool>> Handle(PullAiModelCommand request, CancellationToken ct)
        {
            bool isStarted = await _aiService.StartModelPullAsync(request.model, ct);

            if (!isStarted)
                return ResultDto<bool>.Failure("Downloading a model needs Ollama to be the chosen provider.");

            return ResultDto<bool>.Success(true);
        }
    }

    /// <summary>How the current or last download is going, for a page that has just opened./summary>
    public class GetAiPullStateHandler : IRequestHandler<GetAiPullStateQuery, ResultDto<AiModelPullEventDto?>>
    {
        private readonly IAiModelDownloadService _downloadService;

        public GetAiPullStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<AiModelPullEventDto?>> Handle(GetAiPullStateQuery request, CancellationToken ct)
        {
            return Task.FromResult(ResultDto<AiModelPullEventDto?>.Success(_downloadService.Current));
        }
    }

    /// <summary>Dismisses a finished download. A running one stays, because dismissing it would be a lie.</summary>
    public class ClearAiPullStateHandler : IRequestHandler<ClearAiPullStateCommand, ResultDto<bool>>
    {
        private readonly IAiModelDownloadService _downloadService;

        public ClearAiPullStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<bool>> Handle(ClearAiPullStateCommand request, CancellationToken ct)
        {
            _downloadService.Clear();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
