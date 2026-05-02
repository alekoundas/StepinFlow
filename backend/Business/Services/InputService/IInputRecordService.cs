
namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task<bool> StartRecordingAllAsync();
        public Task<bool> StopRecordingAllAsync();

        public Task<bool> StartRecordingOverlayAsync();
        public Task<bool> StopRecordingOverlayAsync();
    }
}
