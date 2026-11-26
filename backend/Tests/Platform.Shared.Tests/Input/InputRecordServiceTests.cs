using System.Drawing;
using Core.Enums;
using Core.Ports;
using Platform.Shared.Input;

namespace Platform.Shared.Tests.Input
{
    /// <summary>
    /// The global hook of a live desktop session. It only listens, so nothing on the desktop moves. A
    /// headless machine has none, so the trait leaves it out unless -p:Desktop=true asks.
    /// </summary>
    [Trait("Category", "Desktop")]
    public sealed class InputRecordServiceTests
    {
        private static readonly TimeSpan LONG_ENOUGH = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan TO_START = TimeSpan.FromMilliseconds(500);

        private static CancellationToken Ct
        {
            get { return TestContext.Current.CancellationToken; }
        }

        // The host waits for it as the app closes, and only then stops the execution engine.
        [Fact]
        public async Task The_hook_ends_when_it_is_cancelled()
        {
            using CancellationTokenSource stopping = new CancellationTokenSource();
            using InputRecordService service = new InputRecordService(new FakeBroadcastService(), new FakeMouseService(), TimeProvider.System);

            Task hook = service.StartGlobalHookAsync(stopping.Token);
            await Task.Delay(TO_START, Ct);
            hook.IsCompleted.ShouldBeFalse();

            await stopping.CancelAsync();
            await hook.WaitAsync(LONG_ENOUGH, Ct);
        }

        private sealed class FakeBroadcastService : IIpcBroadcastService
        {
            public ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload)
            {
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FakeMouseService : IMouseService
        {
            public Point CursorPosition()
            {
                return Point.Empty;
            }

            public bool MoveCursor(int x, int y)
            {
                return true;
            }

            public uint ScrollPerNotch(bool isHorizontal)
            {
                return 0;
            }
        }
    }
}
