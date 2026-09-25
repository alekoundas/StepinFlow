using Business.Notification;
using Core.Models.Business;

namespace Business.Tests.Fakes
{
    public sealed class FakeDiscordSendQueue : IDiscordSendQueue
    {
        public List<(DiscordMessage Message, TimeSpan MinimumInterval)> Queued { get; } = new List<(DiscordMessage, TimeSpan)>();

        // False is a message inside the bot's rate limit, which the real queue drops.
        public bool Accepts { get; set; } = true;

        public bool Enqueue(DiscordMessage message, TimeSpan minimumInterval)
        {
            if (Accepts)
                Queued.Add((message, minimumInterval));

            return Accepts;
        }
    }
}
