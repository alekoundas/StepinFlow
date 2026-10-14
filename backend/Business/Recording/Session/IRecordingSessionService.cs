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

        /// <summary>
        /// Stops, returns every input captured, in order, and starts working the recording through
        /// in the background. RECORDING_PROGRESS says how far, then READY, and GetResult has it.
        /// Stopped already, it returns the inputs again and starts nothing.
        /// </summary>
        Task<IReadOnlyList<RecordedInput>> StopAsync(CancellationToken ct = default);

        /// <summary>Stops if it has to, and keeps nothing: the inputs and the screenshots go.</summary>
        Task DiscardAsync(CancellationToken ct = default);

        /// <summary>The actions, and what each click hit. Null until the recording is READY.</summary>
        RecordingResultDto? GetResult();

        /// <summary>The screenshot around a press, or null when it never had one.</summary>
        byte[]? GetScreenshot(int index);

        /// <summary>The main area at a press, or null when the session had none.</summary>
        RecordedPress? GetPress(int index);
    }
}
