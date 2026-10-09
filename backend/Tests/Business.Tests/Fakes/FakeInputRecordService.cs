using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>A hook a test drives by hand: what it raises is what the session hears.</summary>
    public sealed class FakeInputRecordService : IInputRecordService
    {
        public event Action<RecordedInput>? ActionRecorded;

        public bool IsRecording { get; private set; }

        public void Raise(RecordedInput input)
        {
            ActionRecorded?.Invoke(input);
        }

        public Task<bool> StartRecordingAllAsync()
        {
            IsRecording = true;
            return Task.FromResult(true);
        }

        public Task<bool> StopRecordingAllAsync()
        {
            IsRecording = false;
            return Task.FromResult(true);
        }

        public Task StartGlobalHookAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopGlobalHookAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartRecordingOverlayAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopRecordingOverlayAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartRecordingPointCaptureAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopRecordingPointCaptureAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartRecordingHotkeyAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> StopRecordingHotkeyAsync()
        {
            throw new NotImplementedException();
        }
    }
}
