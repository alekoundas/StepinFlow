using Business.Ai.Providers;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Business;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// The local model is the base: nothing is on until Ollama answers with the model chosen, and a
    /// cloud model only ever answers in its place.
    /// </summary>
    public sealed class AiProviderServiceTests
    {
        private const string LOCAL_MODEL = "qwen3.5:4b";
        private const string CLOUD_MODEL = "gpt-4o-mini";

        private readonly FakeAppSettingService _settings = new FakeAppSettingService();
        private readonly FakeOllamaService _ollama = new FakeOllamaService();
        private readonly AiProviderService _service;

        public AiProviderServiceTests()
        {
            _service = new AiProviderService(_settings, _ollama);
        }

        private void ChooseLocalModel()
        {
            _settings.Stored[AppSettingKeyEnum.AI_OLLAMA_MODEL] = LOCAL_MODEL;
            _ollama.Models.Add(LOCAL_MODEL);
        }

        private void AddCloudModel(string model = CLOUD_MODEL, string apiKey = "sk-test")
        {
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_PROVIDER] = nameof(AiCloudProviderEnum.OPENAI);
            _settings.Stored[AppSettingKeyEnum.AI_CLOUD_MODEL] = model;
            _settings.Stored[AppSettingKeyEnum.AI_API_KEY] = apiKey;
        }

        // ================================================================
        // Whether AI is on
        // ================================================================

        [Fact]
        public async Task Nothing_is_on_until_a_local_model_is_chosen()
        {
            _ollama.Models.Add(LOCAL_MODEL);

            (await _service.IsConfiguredAsync(Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task Nothing_is_on_while_Ollama_does_not_answer()
        {
            ChooseLocalModel();
            _ollama.IsRunning = false;

            (await _service.IsConfiguredAsync(Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task Nothing_is_on_when_Ollama_does_not_have_the_model_chosen()
        {
            _settings.Stored[AppSettingKeyEnum.AI_OLLAMA_MODEL] = LOCAL_MODEL;
            _ollama.Models.Add("llama3.1:8b");

            (await _service.IsConfiguredAsync(Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task A_cloud_model_does_not_stand_in_for_a_local_one_Ollama_cannot_run()
        {
            ChooseLocalModel();
            AddCloudModel();
            _ollama.IsRunning = false;

            (await _service.IsConfiguredAsync(Ct)).ShouldBeFalse();
        }

        // ================================================================
        // Who answers
        // ================================================================

        [Fact]
        public async Task The_local_model_answers_and_may_see_the_screen()
        {
            ChooseLocalModel();

            AiSettings? settings = await _service.ReadAsync(Ct);

            settings.ShouldNotBeNull();
            settings.CloudProvider.ShouldBe(AiCloudProviderEnum.NONE);
            settings.OllamaModel.ShouldBe(LOCAL_MODEL);
            (await _service.GetModelAsync(Ct)).ShouldBe(LOCAL_MODEL);
            (await _service.CanSendScreenDataAsync(Ct)).ShouldBeTrue();
        }

        [Fact]
        public async Task A_cloud_model_with_a_model_and_a_key_answers_in_its_place_without_seeing_the_screen()
        {
            ChooseLocalModel();
            AddCloudModel();

            AiSettings? settings = await _service.ReadAsync(Ct);

            settings.ShouldNotBeNull();
            settings.CloudProvider.ShouldBe(AiCloudProviderEnum.OPENAI);
            settings.CloudModel.ShouldBe(CLOUD_MODEL);
            (await _service.GetModelAsync(Ct)).ShouldBe(CLOUD_MODEL);
            (await _service.CanSendScreenDataAsync(Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task A_cloud_model_sees_the_screen_once_it_is_allowed_to()
        {
            ChooseLocalModel();
            AddCloudModel();
            _settings.Stored[AppSettingKeyEnum.AI_SEND_SCREEN_CONTENT] = "true";

            (await _service.CanSendScreenDataAsync(Ct)).ShouldBeTrue();
        }

        [Theory]
        [InlineData(CLOUD_MODEL, "")]
        [InlineData("", "sk-test")]
        public async Task A_cloud_model_missing_its_model_or_its_key_leaves_the_local_one_answering(string model, string apiKey)
        {
            ChooseLocalModel();
            AddCloudModel(model, apiKey);

            AiSettings? settings = await _service.ReadAsync(Ct);

            settings.ShouldNotBeNull();
            settings.CloudProvider.ShouldBe(AiCloudProviderEnum.NONE);
            (await _service.GetModelAsync(Ct)).ShouldBe(LOCAL_MODEL);
            (await _service.CanSendScreenDataAsync(Ct)).ShouldBeTrue();
        }
    }
}
