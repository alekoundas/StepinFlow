
using Core.Models.Business;

namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task<bool> StartRecordingAllAsync();
        public Task<bool> StopRecordingAllAsync();

        /// <summary>Drains recorded actions in order. One reader only.</summary>
        public IAsyncEnumerable<RecordedInput> GetActions();

        public Task<bool> StartRecordingOverlayAsync();
        public Task<bool> StopRecordingOverlayAsync();

        // Arms "click anywhere to pick a point": broadcasts button and key events so the form
        // can take the coordinates of the next click without opening a capture window.
        public Task<bool> StartRecordingPointCaptureAsync();
        public Task<bool> StopRecordingPointCaptureAsync();
    }
}
