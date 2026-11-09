namespace Business.FlowScript.Models
{
    /// <summary>
    /// A Discord bot the flow sends through, by the name it has on this machine: a line under
    /// <c>Discord Bots:</c>, shared by every <c>Notify</c> that names it. A bot is a webhook, kept out
    /// of every file, so the file names it and the import finds it.
    /// </summary>
    public sealed class DiscordBotReference
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Its line under Discord Bots, for a warning about it. Zero when it was loaded rather than read.</summary>
        public int Line { get; set; }
    }
}
