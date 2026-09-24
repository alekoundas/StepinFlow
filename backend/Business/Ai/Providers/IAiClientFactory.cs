using Microsoft.Extensions.AI;

namespace Business.Ai.Providers
{
    public interface IAiClientFactory
    {
        Task<IChatClient?> CreateAsync(CancellationToken ct = default);
    }
}
