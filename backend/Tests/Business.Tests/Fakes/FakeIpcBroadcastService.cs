using Core.Enums;
using Core.Ports;

namespace Business.Tests.Fakes
{
    public sealed class FakeIpcBroadcastService : IIpcBroadcastService
    {
        private readonly List<Waiter> _waiters = new List<Waiter>();

        public List<(BroadcastTypeEnum Type, object? Payload)> Sent { get; } = new List<(BroadcastTypeEnum Type, object? Payload)>();

        public ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload)
        {
            lock (Sent)
            {
                Sent.Add((type, payload));

                foreach (Waiter waiter in _waiters.Where(x => x.Match(payload)).ToList())
                {
                    _waiters.Remove(waiter);
                    waiter.Seen.TrySetResult();
                }
            }

            return ValueTask.CompletedTask;
        }

        /// <summary>Done once a payload that matches has been sent, counting one sent already.</summary>
        public Task SentAsync(Func<object?, bool> match)
        {
            lock (Sent)
            {
                if (Sent.Any(x => match(x.Payload)))
                    return Task.CompletedTask;

                TaskCompletionSource seen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add(new Waiter(match, seen));
                return seen.Task;
            }
        }

        private sealed record Waiter(Func<object?, bool> Match, TaskCompletionSource Seen);
    }
}
