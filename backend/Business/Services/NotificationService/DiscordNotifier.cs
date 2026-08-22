using Core.Models.Business;
using Microsoft.Extensions.Logging;
using ProtoBuf.WellKnownTypes;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Business.Services.NotificationService
{
    /// <summary>
    /// Posts a message to a Discord incoming webhook.
    ///
    /// A webhook call is an ordinary POST whose only authentication is the URL itself, so the URL
    /// is never logged - the bot name goes into the log instead.
    /// </summary>
    public class DiscordNotifier : IDiscordNotifier
    {
        public const int MaxContentLength = 2000; // Max content Discord allows.

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DiscordNotifier> _logger;

        public DiscordNotifier(IHttpClientFactory httpClientFactory, ILogger<DiscordNotifier> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendAsync(DiscordMessage message, CancellationToken ct)
        {
            try
            {
                using MultipartFormDataContent form = BuildForm(message);

                HttpClient client = _httpClientFactory.CreateClient(nameof(DiscordNotifier));
                using HttpResponseMessage response = await client.PostAsync(message.WebhookUrl, form, ct);

                if (response.IsSuccessStatusCode)
                    return true;

                // 429 means we sent faster than the throttle allows and the message is already gone.
                _logger.LogWarning("Discord refused a notification from {BotName}: {Status}", Describe(message), response.StatusCode);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Discord notification from {BotName} failed to send.", Describe(message));
                return false;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// Multipart even with no files: one shape to maintain, and Discord accepts payload_json
        /// on its own.
        /// </summary>
        private static MultipartFormDataContent BuildForm(DiscordMessage message)
        {
            MultipartFormDataContent form = new();

            var payload = new
            {
                content = message.Content.Length <= MaxContentLength ? message.Content : message.Content[..MaxContentLength],

                //Null rather than empty
                username = NullIfBlank(message.BotName),
                avatar_url = NullIfBlank(message.AvatarUrl),
            };

            form.Add(
                new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
                "payload_json");

            for (int i = 0; i < message.Attachments.Count; i++)
            {
                DiscordAttachment attachment = message.Attachments[i];

                ByteArrayContent file = new(attachment.Bytes);
                file.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                // The indexed part name is what Discord matches attachments on.
                form.Add(file, $"files[{i}]", attachment.FileName);
            }

            return form;
        }


        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;

        private static string Describe(DiscordMessage message) =>
            string.IsNullOrWhiteSpace(message.BotName) ? "(unnamed bot)" : message.BotName;
    }
}
