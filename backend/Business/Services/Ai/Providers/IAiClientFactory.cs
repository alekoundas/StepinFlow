using Microsoft.Extensions.AI;

namespace Business.Services.Ai.Providers
{
    public interface IAiClientFactory
    {
        Task<IChatClient?> CreateAsync(CancellationToken ct = default);
    }
}
