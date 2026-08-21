
using Core.Models.Business;

namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task<bool> StartRecordingAllAsync();
        public Task<bool> StopRecordingAllAsync();

        /// <summary>
        /// Raised for every recorded input while any mode is active. Handlers run on the hook
        /// thread and must return immediately.
        /// </summary>
        public event Action<RecordedInput>? ActionRecorded;

        public Task<bool> StartRecordingOverlayAsync();
        public Task<bool> StopRecordingOverlayAsync();

        // Arms "click anywhere to pick a point": broadcasts button and key events so the form
        // can take the coordinates of the next click without opening a capture window.
        public Task<bool> StartRecordingPointCaptureAsync();
        public Task<bool> StopRecordingPointCaptureAsync();
    }
}
