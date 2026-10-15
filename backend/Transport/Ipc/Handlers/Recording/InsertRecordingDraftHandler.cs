using Business.Recording.Generation;
using Business.Recording.Session;
using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// After Review, into a flow that already existed: the recording, as the tester reviewed it,
    /// written in at the position the editor was at, with no model. The editor's "Record steps here".
    /// </summary>
    public class InsertRecordingDraftHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly IFlowGenerationService _flowGenerationService;

        public InsertRecordingDraftHandler(IRecordingSessionService recordingSessionService, IFlowGenerationService flowGenerationService)
        {
            _recordingSessionService = recordingSessionService;
            _flowGenerationService = flowGenerationService;
        }

        public async Task<ResultDto<FlowScriptImportResultDto>> HandleAsync(RecordingDraftPositionDto position, CancellationToken ct)
        {
            RecordingResultDto? recording = _recordingSessionService.GetResult();
            if (recording == null)
                return ResultDto<FlowScriptImportResultDto>.Failure("The recording is not ready yet.");

            ReviewedRecordingDto? reviewed = _recordingSessionService.GetReviewed();
            if (reviewed == null)
                return ResultDto<FlowScriptImportResultDto>.Failure("Review the recording first.");

            return await _flowGenerationService.InsertPlainDraftAsync(position, recording, reviewed, _recordingSessionService.GetScreen, ct);
        }
    }
}
