using Core.Enums;

namespace Core.Models.Business
{
    /// <summary>
    /// Everything needed to reach the model that answers. Only ever handed out complete - a null in
    /// its place means the AI features are not set up, which is one answer rather than five fields
    /// to test.
    /// </summary>
    public sealed class AiSettings
    {
        public AiSettings(string ollamaUrl, string ollamaModel, AiCloudProviderEnum cloudProvider, string cloudModel, string apiKey)
        {
            OllamaUrl = ollamaUrl;
            OllamaModel = ollamaModel;
            CloudProvider = cloudProvider;
            CloudModel = cloudModel;
            ApiKey = apiKey;
        }

        public string OllamaUrl { get; }
        public string OllamaModel { get; }

        /// <summary>NONE unless a cloud model has a model and a key, and then it answers in the local one's place.</summary>
        public AiCloudProviderEnum CloudProvider { get; }
        public string CloudModel { get; }
        public string ApiKey { get; }
    }
}
