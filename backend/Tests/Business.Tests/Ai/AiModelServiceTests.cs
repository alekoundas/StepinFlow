using Business.Ai.AiModels;
using Business.Ai.Helpers;
using Business.Ai.Providers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// What the Home page says about the local model, and letting it go.
    /// Test:
    /// 1) The state moves from Ollama answering, to a model chosen, downloaded, then in memory
    /// 2) A name with no tag means the same model as one with :latest
    /// 3) A cloud model is named beside the local model and changes nothing about it
    /// 4) Free memory unloads the local model, and has nothing to unload with none chosen
    /// 5) A model short of tools or vision says why, and every model offered for download has both
    /// </summary>
    public sealed class AiModelServiceTests
    {
        private const string LOCAL_MODEL = "qwen3.5:4b";
        private const string CLOUD_MODEL = "gpt-4o-mini";

        private readonly FakeAppSettingService _settings = new FakeAppSettingService();
        private readonly FakeOllamaService _ollama = new FakeOllamaService();
        private readonly AiModelService _service;

        public AiModelServiceTests()
        {
            AiProviderService provider = new AiProviderService(_settings, _ollama);

            _service = new AiModelService(provider, _ollama, new NoDownloads(), NullLogger<AiModelService>.Instance);
        }

        private void ChooseLocalModel()
        {
            _settings.Stored[AppSettingKeyEnum.AI_OLLAMA_MODEL] = LOCAL_MODEL;
            _ollama.Models.Add(LOCAL_MODEL);
        }

        // ================================================================
        // The state
        // ================================================================

        [Fact]
        public async Task Ollama_not_answering_comes_before_everything_else()
        {
            ChooseLocalModel();
            _ollama.IsRunning = false;

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.NOT_ANSWERING);
            status.Model.ShouldBe(LOCAL_MODEL);
        }

        [Fact]
        public async Task No_model_chosen_is_said_once_Ollama_answers()
        {
            _ollama.Models.Add(LOCAL_MODEL);

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.NOT_CHOSEN);
        }

        [Fact]
        public async Task A_model_Ollama_no_longer_has_is_not_downloaded()
        {
            _settings.Stored[AppSettingKeyEnum.AI_OLLAMA_MODEL] = LOCAL_MODEL;
            _ollama.Models.Add("llama3.1:8b");

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.NOT_DOWNLOADED);
        }

        [Fact]
        public async Task A_downloaded_model_nothing_has_asked_is_not_in_memory()
        {
            ChooseLocalModel();
            _ollama.Loaded.Add(new AiLoadedModelDto { Name = "llama3.1:8b", MemoryBytes = 9_000 });

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.NOT_IN_MEMORY);
            status.MemoryBytes.ShouldBe(0);
        }

        [Fact]
        public async Task A_loaded_model_says_how_much_memory_it_takes()
        {
            ChooseLocalModel();
            _ollama.Loaded.Add(new AiLoadedModelDto { Name = LOCAL_MODEL, MemoryBytes = 5_200_000_000 });

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.IN_MEMORY);
            status.MemoryBytes.ShouldBe(5_200_000_000);
        }

        [Fact]
        public async Task A_name_with_no_tag_is_the_same_model_as_latest()
        {
            _settings.Stored[AppSettingKeyEnum.AI_OLLAMA_MODEL] = "mistral";
            _ollama.Models.Add("mistral:latest");
            _ollama.Loaded.Add(new AiLoadedModelDto { Name = "mistral:latest", MemoryBytes = 1 });

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.State.ShouldBe(AiLocalModelStateEnum.IN_MEMORY);
        }

        // ================================================================
        // A cloud model on top
        // ================================================================

        [Fact]
        public async Task A_cloud_model_is_named_and_the_state_stays_the_local_models()
        {
            ChooseLocalModel();
            _ollama.Loaded.Add(new AiLoadedModelDto { Name = LOCAL_MODEL, MemoryBytes = 7 });
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_PROVIDER] = nameof(AiCloudProviderEnum.OPENAI);
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_MODEL] = CLOUD_MODEL;
            _settings.Stored[AppSettingKeyEnum.AI_API_KEY] = "sk-test";

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.CloudModel.ShouldBe(CLOUD_MODEL);
            status.Model.ShouldBe(LOCAL_MODEL);
            status.State.ShouldBe(AiLocalModelStateEnum.IN_MEMORY);
        }

        [Fact]
        public async Task A_cloud_model_with_no_key_is_not_named()
        {
            ChooseLocalModel();
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_PROVIDER] = nameof(AiCloudProviderEnum.OPENAI);
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_MODEL] = CLOUD_MODEL;

            AiLocalModelStatusDto status = await _service.GetLocalModelStatusAsync(Ct);

            status.CloudModel.ShouldBeEmpty();
        }

        // ================================================================
        // Free memory
        // ================================================================

        [Fact]
        public async Task Free_memory_unloads_the_local_model()
        {
            ChooseLocalModel();

            (await _service.FreeLocalModelAsync(Ct)).ShouldBeTrue();

            _ollama.Unloaded.ShouldBe([LOCAL_MODEL]);
        }

        [Fact]
        public async Task Free_memory_has_nothing_to_unload_with_no_model_chosen()
        {
            (await _service.FreeLocalModelAsync(Ct)).ShouldBeFalse();

            _ollama.Unloaded.ShouldBeEmpty();
        }

        [Fact]
        public async Task Free_memory_says_so_when_Ollama_does_not_answer()
        {
            ChooseLocalModel();
            _ollama.IsRunning = false;

            (await _service.FreeLocalModelAsync(Ct)).ShouldBeFalse();
        }

        // ================================================================
        // What a model needs
        // ================================================================

        [Theory]
        [InlineData(new[] { "completion", "tools" }, "Cannot see images, so it would never be shown a screenshot.")]
        [InlineData(new[] { "completion", "vision" }, "Cannot call tools, so it could not read your flows.")]
        [InlineData(new[] { "embedding" }, "Cannot call tools or see images.")]
        [InlineData(new string[] { }, "Ollama did not say what it can do.")]
        public async Task A_downloaded_model_short_of_tools_or_vision_says_why(string[] capabilities, string reason)
        {
            _ollama.Models.Add(LOCAL_MODEL);
            _ollama.Capabilities.AddRange(capabilities);

            AiModelsDto models = await _service.GetModelsAsync(Ct);

            models.Models.ShouldHaveSingleItem().UnusableReason.ShouldBe(reason);
        }

        [Fact]
        public async Task A_model_that_calls_tools_and_sees_images_can_be_chosen()
        {
            _ollama.Models.Add(LOCAL_MODEL);
            _ollama.Capabilities.AddRange(["completion", "tools", "vision", "thinking"]);

            AiModelsDto models = await _service.GetModelsAsync(Ct);

            models.Models.ShouldHaveSingleItem().UnusableReason.ShouldBeEmpty();
        }

        [Fact]
        public void Every_model_offered_for_download_calls_tools_and_sees_images()
        {
            AvailableAiModelsHelper.Suggestions.ShouldAllBe(x => x.Capabilities.Contains("tools") && x.Capabilities.Contains("vision"));
        }

        // ================================================================
        // Private types
        // ================================================================

        private sealed class NoDownloads : IAiModelDownloadService
        {
            public AiModelDownloadEventDto? Current
            {
                get { return null; }
            }

            public bool Start(string model, string baseUrl)
            {
                return false;
            }

            public void Clear()
            {
            }
        }
    }
}
