
namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartRecordingAllAsync();
        public Task StartRecordingOverlayAsync();
        public Task StopRecordingAsync();
    }
}
