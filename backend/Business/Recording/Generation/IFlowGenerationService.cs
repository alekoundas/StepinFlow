using Core.Models.Dtos;
using Core.Models.Dtos.FlowScript;

namespace Business.Recording.Generation
{
    public interface IFlowGenerationService
    {
        /// <summary>
        /// The reviewed recording added to its flow with no model: built, written as a script, and
        /// imported the way a file is, so what lands in the editor is exactly what that file would
        /// say. The flow's rows are replaced in one go; a draft that does not read cleanly leaves
        /// them as they were and says why.
        /// </summary>
        Task<FlowScriptImportResultDto> ImportPlainDraftAsync(
            int flowId,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct = default);

        /// <summary>
        /// The reviewed recording inserted at a position in a flow that already existed, with no
        /// model. Read back as a script first, as an import is, but only the new rows are written:
        /// replacing the flow's would cost its steps their history. A failure is a position that
        /// is gone; a draft that does not read cleanly is an import's failure, with its lines.
        /// </summary>
        Task<ResultDto<FlowScriptImportResultDto>> InsertPlainDraftAsync(
            RecordingDraftPositionDto position,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            CancellationToken ct = default);
    }
}
