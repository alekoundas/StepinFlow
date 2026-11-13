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

        /// <summary>What every model says it can do: vision, tools.</summary>
        public List<string> Capabilities { get; } = [];

        /// <summary>What Ollama holds in memory.</summary>
        public List<AiLoadedModelDto> Loaded { get; } = [];

        /// <summary>The models it was told to let go, in order.</summary>
        public List<string> Unloaded { get; } = [];

        /// <summary>How many times it was asked what it holds in memory.</summary>
        public int LoadedAsked { get; private set; }

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
            return Task.FromResult(new AiModelDto { Name = model, Capabilities = Capabilities.ToList() });
        }

        public Task<IReadOnlyList<AiLoadedModelDto>?> ListLoadedModelsAsync(string baseUrl, CancellationToken ct = default)
        {
            LoadedAsked++;

            IReadOnlyList<AiLoadedModelDto>? loaded = null;
            if (IsRunning)
                loaded = Loaded;

            return Task.FromResult(loaded);
        }

        public Task<bool> UnloadAsync(string baseUrl, string model, CancellationToken ct = default)
        {
            if (IsRunning)
                Unloaded.Add(model);

            return Task.FromResult(IsRunning);
        }
    }
}
