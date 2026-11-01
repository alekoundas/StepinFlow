using Core.Enums;
using Core.Models.Business;
using Core.Ports;

namespace Business.Tests.Fakes
{
    /// <summary>A hook a test drives by hand: what it raises is what the session hears.</summary>
    public sealed class FakeInputRecordService : IInputRecordService
    {
        public event Action<RecordedInput>? ActionRecorded;
        public event Action<RecordedInput>? PersonKeyChanged;

        public bool IsRecording { get; private set; }

        /// <summary>What the session last asked to hold back.</summary>
        public IReadOnlyList<PressGesture> HeldBack { get; private set; } = [];

        public void Raise(RecordedInput input)
        {
            ActionRecorded?.Invoke(input);
        }

        public int KeyListeners { get; private set; }

        public void StartListeningForKeys()
        {
            KeyListeners++;
        }

        public void StopListeningForKeys()
        {
            KeyListeners--;
        }

        /// <summary>A person pressing a key, by the hook's name for it, such as "VcF9". Heard only while something listens.</summary>
        public void Press(string key)
        {
            if (KeyListeners > 0)
                PersonKeyChanged?.Invoke(new RecordedInput { Type = RecordedInputTypeEnum.KEY_DOWN, KeyChar = key });
        }

        public void Release(string key)
        {
            if (KeyListeners > 0)
                PersonKeyChanged?.Invoke(new RecordedInput { Type = RecordedInputTypeEnum.KEY_UP, KeyChar = key });
        }

        public Task<bool> StartRecordingAllAsync()
        {
            IsRecording = true;
            return Task.FromResult(true);
        }

        public Task<bool> StopRecordingAllAsync()
        {
            IsRecording = false;
            HeldBack = [];
            return Task.FromResult(true);
        }

        public void HoldBackPresses(IReadOnlyList<PressGesture> gestures)
        {
            HeldBack = gestures;
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
