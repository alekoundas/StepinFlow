using System.ClientModel;

using Business.Services.Ai.Helpers;
using Core.Enums;
using Core.Models.Business;

using Microsoft.Extensions.AI;

using OllamaSharp;
using OpenAI;

namespace Business.Services.Ai.Providers
{
    /// <summary>
    /// Turns the settings into a client.
    ///
    /// Local and cloud were the same client with a different address for a while, because Ollama
    /// serves an OpenAI compatible api. That endpoint gives no way to set the context window and
    /// reloads the model at its own 4096 default, which is not enough to hold the system prompt,
    /// eleven tool schemas and a screenshot at the same time. So Ollama gets its own client, talking
    /// to its native api, and everything above this class still only ever sees an IChatClient.
    /// </summary>
    public sealed class AiClientFactory : IAiClientFactory
    {
        private readonly IAiProviderService _providerService;

        public AiClientFactory(IAiProviderService providerService)
        {
            _providerService = providerService;
        }

        public async Task<IChatClient?> CreateAsync(CancellationToken ct = default)
        {
            AiSettings? settings = await _providerService.ReadAsync(ct);
            if (settings == null)
                return null;

            if (settings.Provider == AiProviderEnum.OLLAMA)
            {
                int contextLength = await _providerService.GetOllamaContextLengthAsync(ct);
                OllamaApiClient ollama = new OllamaApiClient(new Uri(settings.OllamaUrl), settings.Model);

                return new OllamaContextChatClient(ollama, contextLength);
            }

            OpenAIClientOptions options = new OpenAIClientOptions();
            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);

            return client.GetChatClient(settings.Model).AsIChatClient();
        }
    }
}
