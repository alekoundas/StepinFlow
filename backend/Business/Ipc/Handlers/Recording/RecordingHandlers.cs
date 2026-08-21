using Business.Services.FlowValidationService;
using Business.Services.RecordingService;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class StartRecordingHandler : IRequestHandler<StartRecordingCommand, ResultDto<bool>>
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public StartRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> Handle(StartRecordingCommand request, CancellationToken ct)
        {
            bool started = await _recordingSessionService.StartAsync(ct);

            return started
                ? ResultDto<bool>.Success(true)
                : ResultDto<bool>.Failure("A recording is already running.");
        }
    }

    /// <summary>
    /// Stops and coalesces in one call, so the wizard opens on a draft rather than on raw input
    /// it would have to interpret a second way.
    /// </summary>
    public class StopRecordingHandler : IRequestHandler<StopRecordingCommand, ResultDto<FlowDraftDto>>
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly IFlowValidator _flowValidator;

        public StopRecordingHandler(
            IRecordingSessionService recordingSessionService,
            IFlowValidator flowValidator)
        {
            _recordingSessionService = recordingSessionService;
            _flowValidator = flowValidator;
        }

        public async Task<ResultDto<FlowDraftDto>> Handle(StopRecordingCommand request, CancellationToken ct)
        {
            IReadOnlyList<RecordedInput> events = await _recordingSessionService.StopAsync(ct);

            FlowDraftDto draft = new FlowDraftDto
            {
                Steps = RecordingDraftBuilder.Build(events),
            };

            DraftValidator.Annotate(draft, _flowValidator);

            return ResultDto<FlowDraftDto>.Success(draft);
        }
    }

    public class DiscardRecordingHandler : IRequestHandler<DiscardRecordingCommand, ResultDto<bool>>
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public DiscardRecordingHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public async Task<ResultDto<bool>> Handle(DiscardRecordingCommand request, CancellationToken ct)
        {
            if (_recordingSessionService.IsRecording)
                await _recordingSessionService.StopAsync(ct);

            _recordingSessionService.Clear();
            return ResultDto<bool>.Success(true);
        }
    }

    public class GetRecordingScreenshotHandler : IRequestHandler<GetRecordingScreenshotQuery, ResultDto<byte[]>>
    {
        private readonly IRecordingSessionService _recordingSessionService;

        public GetRecordingScreenshotHandler(IRecordingSessionService recordingSessionService)
        {
            _recordingSessionService = recordingSessionService;
        }

        public Task<ResultDto<byte[]>> Handle(GetRecordingScreenshotQuery request, CancellationToken ct)
        {
            byte[]? image = _recordingSessionService.GetScreenshot(request.index);

            return Task.FromResult(image == null
                ? ResultDto<byte[]>.Failure("That action has no screenshot.")
                : ResultDto<byte[]>.Success(image));
        }
    }
}
