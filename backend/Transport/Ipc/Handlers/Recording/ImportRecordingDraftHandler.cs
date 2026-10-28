using Business.Recording.Generation;
using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// After Review: the recording, as the tester reviewed it, written into its flow with no model,
    /// with what was saved for the Check page. The flow is the one the setup created, which the page
    /// knows and the session does not.
    /// </summary>
    public class ImportRecordingDraftHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly IFlowGenerationService _flowGenerationService;

        public ImportRecordingDraftHandler(IRecordingSessionService recordingSessionService, IFlowGenerationService flowGenerationService)
        {
            _recordingSessionService = recordingSessionService;
            _flowGenerationService = flowGenerationService;
        }

        public async Task<ResultDto<FlowGenerationResultDto>> HandleAsync(int flowId, CancellationToken ct)
        {
            RecordingResultDto? recording = _recordingSessionService.GetResult();
            if (recording == null)
                return ResultDto<FlowGenerationResultDto>.Failure("The recording is not ready yet.");

            ReviewedRecordingDto? reviewed = _recordingSessionService.GetReviewed();
            if (reviewed == null)
                return ResultDto<FlowGenerationResultDto>.Failure("Review the recording first.");

            return await _flowGenerationService.ImportPlainDraftAsync(flowId, recording, reviewed, _recordingSessionService.GetScreen, ct);
        }
    }
}
