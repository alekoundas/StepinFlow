using Microsoft.Extensions.AI;

namespace Business.Ai.Providers
{
    public interface IAiClientFactory
    {
        Task<IChatClient?> CreateAsync(CancellationToken ct = default);

        /// <summary>
        /// The local model's client even when a cloud model answers in its place: for looking at
        /// screenshots, which never leave the machine. Null when nothing is set up.
        /// </summary>
        Task<IChatClient?> CreateLocalAsync(CancellationToken ct = default);
    }
}
