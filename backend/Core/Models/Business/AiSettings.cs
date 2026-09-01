using Core.Enums;

namespace Core.Models.Business
{
    /// <summary>
    /// Everything needed to reach a model. Only ever handed out complete - a null in its place
    /// means the AI features are not set up, which is one answer rather than four fields to test.
    /// </summary>
    public sealed record AiSettings(AiProviderEnum Provider, string Model, string ApiKey, string OllamaUrl);
}
