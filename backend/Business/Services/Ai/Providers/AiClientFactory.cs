using System.ClientModel;
using Business.Services.Ai.Helpers;
using Core.Enums;
using Core.Models.Business;

using Microsoft.Extensions.AI;
using OpenAI;

namespace Business.Services.Ai.Providers
{
    /// <summary>
    /// Turns the settings into a client.
    ///
    /// Local and cloud are the same client with a different address: Ollama serves an OpenAI
    /// compatible api, so pointing at localhost is the whole of "run it on this machine". Nothing
    /// above this class ever learns which one it got.
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

            OpenAIClientOptions options = new OpenAIClientOptions();

            if (settings.Provider == AiProviderEnum.OLLAMA)
                options.Endpoint = new Uri(OllamaUrlHelper.ToOpenAiEndpoint(settings.OllamaUrl));

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
            return client.GetChatClient(settings.Model).AsIChatClient();
        }
    }
}
