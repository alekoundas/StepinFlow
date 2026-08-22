using Core.Models.Business;

namespace Business.Services.NotificationService
{
    public interface IDiscordNotifier
    {
        /// <summary>Posts now. Never throws.</summary>
        Task<bool> SendAsync(DiscordMessage message, CancellationToken ct);
    }

    public interface IDiscordSendQueue
    {
        /// <summary>
        /// Hands the message to a background sender and returns immediately. Returns false when the
        /// bot's throttle window has not elapsed, in which case the message is discarded.
        /// </summary>
        /// <param name="minimumInterval">
        /// The bot's configured gap. Pass TimeSpan.Zero only for something the user asked for
        /// directly, never for a step - a caller that skips the throttle is what gets a webhook
        /// revoked.
        /// </param>
        bool Enqueue(DiscordMessage message, TimeSpan minimumInterval);
    }
}
