using Business.Ai.Helpers;
using Business.Ai.Providers;
using Core.Models.Dtos;

namespace Business.Tests.Fakes
{
    /// <summary>An Ollama with the models a test downloads into it, or one that does not answer at all.</summary>
    public sealed class FakeOllamaService : IOllamaService
    {
        public bool IsRunning { get; set; } = true;
        public List<string> Models { get; } = [];

        public Task<IReadOnlyList<string>?> ListModelsAsync(string baseUrl, CancellationToken ct = default)
        {
            IReadOnlyList<string>? models = null;
            if (IsRunning)
                models = Models;

            return Task.FromResult(models);
        }

        public Task<bool> HasModelAsync(string baseUrl, string model, CancellationToken ct = default)
        {
            string wanted = OllamaUrlHelper.NormaliseModelName(model);

            return Task.FromResult(IsRunning && Models.Any(x => OllamaUrlHelper.NormaliseModelName(x) == wanted));
        }

        public Task<AiModelDto> DescribeAsync(string baseUrl, string model, CancellationToken ct = default)
        {
            return Task.FromResult(new AiModelDto { Name = model });
        }
    }
}
