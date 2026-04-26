
namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartGlobalHookAsync();
        public Task StopGlobalHookAsync();

        public Task StartRecordingAllAsync();
        public Task StopRecordingAllAsync();

        public Task StartRecordingOverlayAsync();
        public Task StopRecordingOverlayAsync();
    }
}
