using Business.FlowScript.Models;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Recording.Draft
{
    public interface IRecordingDraftBuilder
    {
        /// <summary>
        /// The reviewed recording added to the flow's rows, as a flow of its own with no model and no
        /// questions: child number index of under, or of the flow's top when under is null, or
        /// after the steps already there when index is null. Templates carry their images, cut from
        /// the main area at each press. screenAt is the recording session's: an action's own press,
        /// or the last click's before it.
        /// </summary>
        FlowScriptSchema Build(
            FlowScriptSchema flow,
            RecordingResultDto recording,
            ReviewedRecordingDto reviewed,
            Func<int, RecordingScreenDto?> screenAt,
            FlowStep? under,
            int? index);
    }
}
