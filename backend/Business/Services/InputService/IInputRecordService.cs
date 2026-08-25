
using Core.Models.Business;

namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        /// <summary>
        /// Raised for every recorded input while any mode is active. Handlers run on the hook
        /// thread and must return immediately.
        /// </summary>
        public event Action<RecordedInput>? ActionRecorded;

        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task<bool> StartRecordingAllAsync();
        public Task<bool> StopRecordingAllAsync();


        public Task<bool> StartRecordingOverlayAsync();
        public Task<bool> StopRecordingOverlayAsync();

        // Broadcasts button and key events 
        public Task<bool> StartRecordingPointCaptureAsync();
        public Task<bool> StopRecordingPointCaptureAsync();

        // Broadcasts every key 
        public Task<bool> StartRecordingHotkeyAsync();
        public Task<bool> StopRecordingHotkeyAsync();
    }
}
