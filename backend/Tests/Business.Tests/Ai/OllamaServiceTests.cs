using System.Net;
using System.Text;
using System.Text.Json;
using Business.Ai.Providers;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// What is sent to Ollama and read back. The replies are the shape Ollama's own api gives.
    /// Test:
    /// 1) /api/ps gives each model's name and size, an empty list when nothing is loaded
    /// 2) An unload posts the model with a keep_alive of 0 and no prompt, which is what makes it one
    /// 3) Ollama not answering is an answer, not an exception
    /// </summary>
    public sealed class OllamaServiceTests : IDisposable
    {
        private const string BASE_URL = "http://localhost:11434";

        private readonly StubHandler _handler = new StubHandler();
        private readonly OllamaService _service;

        public OllamaServiceTests()
        {
            _service = new OllamaService(new StubFactory(_handler), NullLogger<OllamaService>.Instance);
        }

        public void Dispose()
        {
            _handler.Dispose();
        }

        [Fact]
        public async Task A_loaded_model_is_read_with_its_size()
        {
            _handler.Reply = """{"models":[{"name":"qwen3.5:4b","model":"qwen3.5:4b","size":5200000000,"size_vram":5200000000,"expires_at":"2026-11-05T10:00:00Z"}]}""";

            IReadOnlyList<AiLoadedModelDto>? loaded = await _service.ListLoadedModelsAsync(BASE_URL, Ct);

            _handler.Requested.ShouldBe("GET http://localhost:11434/api/ps");
            loaded.ShouldNotBeNull();
            loaded.Single().Name.ShouldBe("qwen3.5:4b");
            loaded.Single().MemoryBytes.ShouldBe(5_200_000_000);
        }

        [Fact]
        public async Task Nothing_loaded_is_an_empty_list()
        {
            _handler.Reply = """{"models":[]}""";

            IReadOnlyList<AiLoadedModelDto>? loaded = await _service.ListLoadedModelsAsync(BASE_URL, Ct);

            loaded.ShouldNotBeNull();
            loaded.ShouldBeEmpty();
        }

        [Fact]
        public async Task Ollama_not_answering_lists_nothing_at_all()
        {
            _handler.IsAnswering = false;

            (await _service.ListLoadedModelsAsync(BASE_URL, Ct)).ShouldBeNull();
        }

        [Fact]
        public async Task An_unload_is_the_model_with_a_keep_alive_of_0_and_no_prompt()
        {
            _handler.Reply = """{"model":"qwen3.5:4b","done":true,"done_reason":"unload"}""";

            (await _service.UnloadAsync(BASE_URL, "qwen3.5:4b", Ct)).ShouldBeTrue();

            _handler.Requested.ShouldBe("POST http://localhost:11434/api/generate");

            using JsonDocument body = JsonDocument.Parse(_handler.Body);
            body.RootElement.GetProperty("model").GetString().ShouldBe("qwen3.5:4b");
            body.RootElement.GetProperty("keep_alive").GetInt32().ShouldBe(0);
            body.RootElement.TryGetProperty("prompt", out JsonElement _).ShouldBeFalse();
        }

        [Fact]
        public async Task An_unload_Ollama_refuses_is_not_agreed()
        {
            _handler.Status = HttpStatusCode.InternalServerError;

            (await _service.UnloadAsync(BASE_URL, "qwen3.5:4b", Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task An_unload_with_Ollama_not_answering_is_not_agreed()
        {
            _handler.IsAnswering = false;

            (await _service.UnloadAsync(BASE_URL, "qwen3.5:4b", Ct)).ShouldBeFalse();
        }

        // ================================================================
        // Private types
        // ================================================================

        private sealed class StubHandler : HttpMessageHandler
        {
            public bool IsAnswering { get; set; } = true;
            public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
            public string Reply { get; set; } = "{}";
            public string Requested { get; private set; } = string.Empty;
            public string Body { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (!IsAnswering)
                    throw new HttpRequestException("No connection could be made.");

                Requested = $"{request.Method} {request.RequestUri}";

                if (request.Content != null)
                    Body = await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(Status)
                {
                    Content = new StringContent(Reply, Encoding.UTF8, "application/json"),
                };
            }
        }

        private sealed class StubFactory : IHttpClientFactory
        {
            private readonly StubHandler _handler;

            public StubFactory(StubHandler handler)
            {
                _handler = handler;
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient(_handler, disposeHandler: false);
            }
        }
    }
}
