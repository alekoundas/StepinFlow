using Core.Enums;
using Core.Ports;

namespace Business.Tests.Fakes
{
    public sealed class FakeIpcBroadcastService : IIpcBroadcastService
    {
        public List<(BroadcastTypeEnum Type, object? Payload)> Sent { get; } = new List<(BroadcastTypeEnum Type, object? Payload)>();

        public ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload)
        {
            lock (Sent)
                Sent.Add((type, payload));

            return ValueTask.CompletedTask;
        }
    }
}
