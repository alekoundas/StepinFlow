using Business.Ai.Providers;
using Microsoft.Extensions.AI;

namespace Business.Tests.Fakes
{
    /// <summary>Null is no model set up.</summary>
    public sealed class FakeAiClientFactory : IAiClientFactory
    {
        public IChatClient? Client { get; set; }

        public Task<IChatClient?> CreateAsync(CancellationToken ct = default)
        {
            return Task.FromResult(Client);
        }
    }
}
