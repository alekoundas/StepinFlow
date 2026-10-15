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

        /// <summary>What the last generation saved. Null until one is DONE, and after a cancel.</summary>
        FlowGenerationResultDto? GetResult();

        /// <summary>Stops one that is going and saves nothing, then says NONE.</summary>
        Task CancelAsync();
    }
}
