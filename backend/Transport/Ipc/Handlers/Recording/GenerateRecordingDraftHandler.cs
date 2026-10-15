using Business.Recording.Generation;
using Business.Recording.Session;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// After Review: the recording, as the tester reviewed it, shaped by the model into the flow the
    /// setup created. Starts it and returns; FLOW_GENERATION_PROGRESS says how far.
    /// </summary>
    public class GenerateRecordingDraftHandler
    {
        private readonly IRecordingSessionService _recordingSessionService;
        private readonly IFlowGenerationSessionService _generationSessionService;

        public GenerateRecordingDraftHandler(IRecordingSessionService recordingSessionService, IFlowGenerationSessionService generationSessionService)
        {
            _recordingSessionService = recordingSessionService;
            _generationSessionService = generationSessionService;
        }

        public async Task<ResultDto<bool>> HandleAsync(int flowId, CancellationToken ct)
        {
            RecordingResultDto? recording = _recordingSessionService.GetResult();
            if (recording == null)
                return ResultDto<bool>.Failure("The recording is not ready yet.");

            ReviewedRecordingDto? reviewed = _recordingSessionService.GetReviewed();
            if (reviewed == null)
                return ResultDto<bool>.Failure("Review the recording first.");

            return await _generationSessionService.StartAsync(flowId, recording, reviewed, _recordingSessionService.GetScreen);
        }
    }
}
