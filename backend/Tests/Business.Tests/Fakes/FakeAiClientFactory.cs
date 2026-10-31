using Business.Ai.Providers;
using Microsoft.Extensions.AI;

namespace Business.Tests.Fakes
{
    /// <summary>Null is no model set up. The local model is a client of its own, none unless a test gives one.</summary>
    public sealed class FakeAiClientFactory : IAiClientFactory
    {
        public IChatClient? Client { get; set; }
        public IChatClient? LocalClient { get; set; }

        public Task<IChatClient?> CreateAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Client);
        }

        public Task<IChatClient?> CreateLocalAsync(CancellationToken ct = default)
        {
            return Task.FromResult(LocalClient);
        }
    }
}
