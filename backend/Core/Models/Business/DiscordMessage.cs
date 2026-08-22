namespace Core.Models.Business
{
    /// <summary>One outgoing Discord message, already assembled.</summary>
    public class DiscordMessage
    {
        public int DiscordBotId { get; set; }

        public string WebhookUrl { get; set; } = string.Empty;
        public string BotName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<DiscordAttachment> Attachments { get; set; } = new();
    }

    public class DiscordAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Bytes { get; set; } = [];
    }
}
