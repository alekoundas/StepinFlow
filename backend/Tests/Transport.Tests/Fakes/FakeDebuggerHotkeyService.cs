using Business.Executions;

namespace Transport.Tests.Fakes
{
    /// <summary>Counts the times the keys were read, and says whether it was asked to listen.</summary>
    public sealed class FakeDebuggerHotkeyService : IDebuggerHotkeyService
    {
        public int BindCount { get; private set; }
        public bool WasListening { get; private set; }

        public Task BindAsync(CancellationToken ct)
        {
            BindCount++;
            return Task.CompletedTask;
        }

        public Task ListenUntilAsync(Task executionCompletion)
        {
            WasListening = true;
            return Task.CompletedTask;
        }
    }
}
