
using Core.Models.Business;

namespace Core.Ports
{
    // Dependency Inversion Principle(DIP)
    public interface IInputRecordService
    {
        /// <summary>
        /// Raised for every recorded input while any mode is active. 
        /// Handlers run on the hook thread and must return immediately.
        /// </summary>
        public event Action<RecordedInput>? ActionRecorded;

        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task<bool> StartRecordingAllAsync();
        public Task<bool> StopRecordingAllAsync();

        /// <summary>
        /// While recording, a press matching one of these is held back from the application, and
        /// so is its release; both are still raised, with <see cref="RecordedInput.Gesture"/> set.
        /// Empty holds nothing back. Windows and macOS only: on Linux the press goes through too.
        /// </summary>
        public void HoldBackPresses(IReadOnlyList<PressGesture> gestures);


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
