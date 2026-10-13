using Business.Recording.Models.Session;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Business.Recording.Session
{
    public interface IRecordingSessionService
    {
        bool IsRecording { get; }
        bool IsPaused { get; }

        /// <summary>
        /// Records against the flow's main area, which has to be on screen. With no flow it records
        /// as the wizard always has: no main area, and each screenshot taken after its press.
        /// Nothing done on the windows of electronProcessId, StepinFlow's own, is recorded.
        /// </summary>
        Task<ResultDto<bool>> StartAsync(int? flowId, int electronProcessId, CancellationToken ct = default);

        /// <summary>Stops recording without ending the session.</summary>
        void Pause();

        void Resume();

        /// <summary>Stops and returns every input captured, in order. Stopped already, it returns them again.</summary>
        Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default);

        /// <summary>Stops if it has to, and keeps nothing: the inputs and the screenshots go.</summary>
        Task DiscardAsync(CancellationToken ct = default);



        /// <summary>The inputs folded into actions, as of the stop for a recording that has stopped.</summary>
        IReadOnlyList<RecordedActionDto> GetActions();

        /// <summary>Where the recording is, for a page that opens after it started.</summary>
        RecordingProgressDto GetProgress();

        /// <summary>The screenshot around a press, or null when it never had one.</summary>
        byte[]? GetScreenshot(int index);

        /// <summary>The main area at a press, or null when the session had none.</summary>
        RecordedPress? GetPress(int index);
    }
}
