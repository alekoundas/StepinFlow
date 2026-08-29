using System.Collections.Concurrent;
using System.Threading.Channels;

using Core.Models.Business;
using Microsoft.Extensions.Logging;

namespace Business.Services.NotificationService
{
    /// <summary>
    /// Throttles by the clock and sends off the caller's thread.
    ///
    /// Two separate jobs, deliberately not one. The throttle is decided at Enqueue so an excess
    /// message is discarded immediately rather than queued and delivered minutes later, long after
    /// the run that produced it. The channel exists only so the HTTP call never happens on the
    /// thread running the flow - if the send were awaited there, a retry loop would run at the
    /// network's pace instead of its own.
    ///
    /// A dropped message is accepted, by design. The case this protects against is a Notify step
    /// inside a failure branch inside a retry loop, where the hundredth copy of an identical alert
    /// is worth less than keeping the webhook alive.
    /// </summary>
    public class DiscordSendQueue : IDiscordSendQueue, IAsyncDisposable
    {
        private readonly Channel<DiscordMessage> _channel = Channel.CreateUnbounded<DiscordMessage>(
            new UnboundedChannelOptions { SingleReader = true });

        private readonly ConcurrentDictionary<int, DateTime> _lastSentByBot = new ConcurrentDictionary<int, DateTime>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _pump;

        private readonly IDiscordNotifier _notifier;
        private readonly ILogger<DiscordSendQueue> _logger;

        public DiscordSendQueue(IDiscordNotifier notifier, ILogger<DiscordSendQueue> logger)
        {
            _notifier = notifier;
            _logger = logger;

            _pump = Task.Run(() => PumpAsync(_cts.Token));
        }

        // ================================================================
        // Public methods
        // ================================================================
        public bool Enqueue(DiscordMessage message, TimeSpan minimumInterval)
        {
            if (minimumInterval > TimeSpan.Zero && !TryClaimSlot(message.DiscordBotId, minimumInterval))
                return false;

            return _channel.Writer.TryWrite(message);
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Writer.TryComplete();
            await _cts.CancelAsync();

            try
            {
                await _pump;
            }
            catch (OperationCanceledException)
            {
                // Shutdown, not a fault.
            }

            _cts.Dispose();
            GC.SuppressFinalize(this);
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// Atomic so two steps failing at once cannot both decide the window is clear.
        /// </summary>
        private bool TryClaimSlot(int discordBotId, TimeSpan minimumInterval)
        {
            DateTime now = DateTime.UtcNow;
            bool claimed = false;

            _lastSentByBot.AddOrUpdate(
                discordBotId,
                _ =>
                {
                    claimed = true;
                    return now;
                },
                (_, previous) =>
                {
                    if (now - previous < minimumInterval)
                        return previous;

                    claimed = true;
                    return now;
                });

            return claimed;
        }

        private async Task PumpAsync(CancellationToken ct)
        {
            await foreach (DiscordMessage message in _channel.Reader.ReadAllAsync(ct))
                await _notifier.SendAsync(message, ct);
        }
    }
}
