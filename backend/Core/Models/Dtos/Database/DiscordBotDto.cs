namespace Core.Models.Dtos
{
    public class DiscordBotDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public string BotName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int RateLimitSeconds { get; set; } = 10;

        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        /// <summary>Steps wired to this bot. Deleting one that is in use is refused.</summary>
        public int FlowStepsCount { get; set; }
    }

    /// <summary>A one-off send straight from the settings form, before anything is saved.</summary>
    public class TestDiscordBotDto
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public string BotName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
