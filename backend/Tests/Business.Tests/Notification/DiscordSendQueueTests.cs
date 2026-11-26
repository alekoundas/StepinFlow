using Business.Notification;
using Core.Models.Business;
using Microsoft.Extensions.Logging.Abstractions;

namespace Business.Tests.Notification
{
    public sealed class DiscordSendQueueTests
    {
        // As .NET asks of any dispose.
        [Fact]
        public async Task Disposing_it_twice_does_not_throw()
        {
            DiscordSendQueue queue = new DiscordSendQueue(new SilentNotifier(), TimeProvider.System, NullLogger<DiscordSendQueue>.Instance);
            await queue.DisposeAsync();

            await Should.NotThrowAsync(queue.DisposeAsync().AsTask());
        }

        private sealed class SilentNotifier : IDiscordNotifier
        {
            public Task<bool> SendAsync(DiscordMessage message, CancellationToken ct)
            {
                return Task.FromResult(true);
            }
        }
    }
}
