using Business.FlowScript.Models;
using Core.Models.Dtos;

namespace Business.Recording.Draft
{
    public interface IRecordingDraftBuilder
    {
        /// <summary>
        /// The reviewed recording added to the flow's rows, after the steps it already has, as a
        /// flow of its own with no model and no questions. Templates carry their images, cut from
        /// the main area at each press. screenAt is the recording session's: an action's own
        /// press, or the last click's before it.
        /// </summary>
        FlowScriptSchema Build(
            FlowScriptSchema flow,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt);
    }
}
