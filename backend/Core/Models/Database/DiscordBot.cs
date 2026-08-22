using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    /// <summary>
    /// A saved way of posting to Discord.
    ///
    /// A "bot" here is a Discord incoming webhook plus the identity it posts under. The webhook
    /// URL is the whole of the authentication - there is no header and no token exchange, so the
    /// URL is a credential and is masked wherever it is listed rather than displayed.
    ///
    /// Two rows may point at the same channel with different names and avatars, which is why this
    /// is a sender rather than a channel: what distinguishes one from another is who the message
    /// appears to come from.
    /// </summary>
    public class DiscordBot : BaseDbModel
    {
        /// <summary>What the user calls it in dropdowns. Not what Discord displays.</summary>
        public string Name { get; set; } = string.Empty;

        public string WebhookUrl { get; set; } = string.Empty;

        /// <summary>
        /// Overrides the display name per message. Empty leaves whatever the webhook was set up
        /// with in Discord.
        /// </summary>
        public string BotName { get; set; } = string.Empty;

        /// <summary>
        /// A URL Discord fetches itself, so a local file cannot be used here.
        /// </summary>
        public string AvatarUrl { get; set; } = string.Empty;

        /// <summary>
        /// Shortest gap between two messages through this bot. Anything arriving inside the gap is
        /// dropped rather than queued, so a Notify step inside a retry loop sends once instead of
        /// a hundred times and gets the webhook revoked.
        /// </summary>
        public int RateLimitSeconds { get; set; } = DefaultRateLimitSeconds;

        public const int DefaultRateLimitSeconds = 10;
        public const int MinRateLimitSeconds = 2;
        public const int MaxRateLimitSeconds = 300;

        public IEnumerable<FlowStep> FlowSteps { get; set; } = new Collection<FlowStep>();
    }
}
