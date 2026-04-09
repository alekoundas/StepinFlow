
namespace Business.Services.InputService
{
    public interface IInputRecordService
    {
        public Task StartRecordingAsync();
        public Task StopRecordingAsync();
    }
}
