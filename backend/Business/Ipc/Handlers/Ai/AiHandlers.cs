using Business.Services.Ai;
using Business.Services.Ai.AiModels;
using Business.Services.Ai.Providers;
using Core.Models.Dtos;
using Core.Models.Ipc;

using MediatR;

namespace Business.Ipc.Handlers.Ai
{
    /// <summary>Reads a run and says what went wrong.</summary>
    public class ExplainExecutionHandler : IRequestHandler<ExplainExecutionQuery, ResultDto<AiAnswerDto>>
    {
        private readonly IExecutionRunExplainService _runExplainService;

        public ExplainExecutionHandler(IExecutionRunExplainService runExplainService)
        {
            _runExplainService = runExplainService;
        }

        public async Task<ResultDto<AiAnswerDto>> Handle(ExplainExecutionQuery request, CancellationToken ct)
        {
            AiAnswerDto answer = await _runExplainService.ExplainExecutionAsync(request.executionId, ct);
            return ResultDto<AiAnswerDto>.Success(answer);
        }
    }

    /// <summary>Whether a provider is set up, so the page can offer the button or explain why not.</summary>
    public class GetAiStatusHandler : IRequestHandler<GetAiStatusQuery, ResultDto<bool>>
    {
        private readonly IAiProviderService _providerService;

        public GetAiStatusHandler(IAiProviderService providerService)
        {
            _providerService = providerService;
        }

        public async Task<ResultDto<bool>> Handle(GetAiStatusQuery request, CancellationToken ct)
        {
            bool isConfigured = await _providerService.IsConfiguredAsync(ct);
            return ResultDto<bool>.Success(isConfigured);
        }
    }

    /// <summary>
    /// Starts a download and comes straight back. It runs for minutes, so how it is going arrives
    /// on the broadcast pipe rather than on this call.
    /// </summary>
    public class DownloadAiModelHandler : IRequestHandler<DownloadAiModelCommand, ResultDto<bool>>
    {
        private readonly IAiModelService _modelService;

        public DownloadAiModelHandler(IAiModelService modelService)
        {
            _modelService = modelService;
        }

        public async Task<ResultDto<bool>> Handle(DownloadAiModelCommand request, CancellationToken ct)
        {
            bool isStarted = await _modelService.StartModelDownloadAsync(request.model, ct);

            if (!isStarted)
                return ResultDto<bool>.Failure("Downloading a model needs Ollama to be the chosen provider.");

            return ResultDto<bool>.Success(true);
        }
    }

    /// <summary>Whether the chat can be offered, and why not when it cannot.</summary>
    public class GetAiChatAvailabilityHandler : IRequestHandler<GetAiChatAvailabilityQuery, ResultDto<AiChatAvailabilityDto>>
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public GetAiChatAvailabilityHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAvailabilityDto>> Handle(GetAiChatAvailabilityQuery request, CancellationToken ct)
        {
            return ResultDto<AiChatAvailabilityDto>.Success(await _flowQuestionService.GetAvailabilityAsync(ct));
        }
    }

    /// <summary>Answers a question about the flows, by letting the model query the database.</summary>
    public class AskAiHandler : IRequestHandler<AskAiQuery, ResultDto<AiChatAnswerDto>>
    {
        private readonly IFlowQuestionService _flowQuestionService;

        public AskAiHandler(IFlowQuestionService flowQuestionService)
        {
            _flowQuestionService = flowQuestionService;
        }

        public async Task<ResultDto<AiChatAnswerDto>> Handle(AskAiQuery request, CancellationToken ct)
        {
            return ResultDto<AiChatAnswerDto>.Success(await _flowQuestionService.AskAsync(request.dto, ct));
        }
    }

    /// <summary>How the current or last download is going, for a page that has just opened./summary>
    public class GetAiDownloadStateHandler : IRequestHandler<GetAiDownloadStateQuery, ResultDto<AiModelDownloadEventDto?>>
    {
        private readonly IAiModelDownloadService _downloadService;

        public GetAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<AiModelDownloadEventDto?>> Handle(GetAiDownloadStateQuery request, CancellationToken ct)
        {
            return Task.FromResult(ResultDto<AiModelDownloadEventDto?>.Success(_downloadService.Current));
        }
    }

    /// <summary>Dismisses a finished download. A running one stays, because dismissing it would be a lie.</summary>
    public class ClearAiDownloadStateHandler : IRequestHandler<ClearAiDownloadStateCommand, ResultDto<bool>>
    {
        private readonly IAiModelDownloadService _downloadService;

        public ClearAiDownloadStateHandler(IAiModelDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public Task<ResultDto<bool>> Handle(ClearAiDownloadStateCommand request, CancellationToken ct)
        {
            _downloadService.Clear();
            return Task.FromResult(ResultDto<bool>.Success(true));
        }
    }
}
