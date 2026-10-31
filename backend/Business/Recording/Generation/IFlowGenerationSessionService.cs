using Core.Models.Dtos;

namespace Business.Recording.Generation
{
    public interface IFlowGenerationSessionService
    {
        /// <summary>
        /// Starts the model's draft of a reviewed recording into its flow, in the background, and
        /// returns. FLOW_GENERATION_PROGRESS says how far, then DONE, and GetResult has what was
        /// saved. Refused while one is going.
        /// </summary>
        Task<ResultDto<bool>> StartAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt);

        /// <summary>
        /// Starts the Check page's "Ask for changes" the same way: the flow as it is, revised by the
        /// model as the tester wrote, with the checks not yet kept or dropped still offered and held
        /// to the recording's screen lines. Refused while a generation is going.
        /// </summary>
        Task<ResultDto<bool>> StartRevisionAsync(RecordingDraftRevisionDto revision, IReadOnlyList<string> screenLines);

        /// <summary>
        /// Sends the request shown as WAITING_TO_SEND, and the generation goes on. Refused when none
        /// is waiting.
        /// </summary>
        ResultDto<bool> Send();

        /// <summary>What the last generation saved. Null until one is DONE, and after a cancel.</summary>
        FlowGenerationResultDto? GetResult();

        /// <summary>Stops one that is going and saves nothing, then says NONE.</summary>
        Task CancelAsync();
    }
}
