using Business.Services.NotificationService;
using Core.Models.Business;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Sends one message straight away, skipping both the queue and the throttle.
    ///
    /// Deliberate: a bot set to 300 seconds would otherwise be untestable, and a diagnostic click
    /// that silently discarded the message would read as a broken webhook. The button's own two
    /// second cooldown is the only limit on this, and it lives in the UI where it is visible.
    ///
    /// Takes the values off the form rather than an id, so a webhook can be checked before it is
    /// ever saved.
    /// </summary>
    public class TestDiscordBotHandler
    {
        private readonly IDiscordNotifier _notifier;

        public TestDiscordBotHandler(IDiscordNotifier notifier)
        {
            _notifier = notifier;
        }

        public async Task<ResultDto<bool>> HandleAsync(TestDiscordBotDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.WebhookUrl))
                return ResultDto<bool>.Failure("Paste the webhook URL first.");

            bool sent = await _notifier.SendAsync(new DiscordMessage
            {
                WebhookUrl = dto.WebhookUrl.Trim(),
                BotName = dto.BotName,
                AvatarUrl = dto.AvatarUrl,
                Content = "**StepinFlow** — test message. If you can read this, the webhook works.",
            }, ct);

            if (!sent)
                return ResultDto<bool>.Failure(
                    "Discord would not take the message. Check the URL is a webhook URL and has not been deleted.");

            return ResultDto<bool>.Success(true);
        }
    }
}
