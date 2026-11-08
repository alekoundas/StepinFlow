using System.Net;
using System.Security.Cryptography;
using System.Text;
using Business.ScreenText;
using Core.Catalogs;
using Core.Models.Business.Ocr;
using Microsoft.Extensions.Logging.Abstractions;
using static Business.Tests.TestToken;

namespace Business.Tests.ScreenText
{
    /// <summary>
    /// A text reader downloaded from the app's release into a folder of its own.
    /// Test:
    /// 1) The model and its list are both kept, under the reader's own names
    /// 2) A model that is not the one expected is kept nowhere
    /// 3) A reader missing from the release says where it looked
    /// 4) The reader built in is never downloaded
    /// </summary>
    public sealed class OcrReaderDownloadServiceTests : IDisposable
    {
        private static readonly byte[] MODEL = Encoding.ASCII.GetBytes("a reader's model");
        private static readonly byte[] CHARACTERS = Encoding.ASCII.GetBytes("character_dict:\n  - a\n");

        private readonly string _folder = Path.Combine(Path.GetTempPath(), "StepinFlowTests", Guid.NewGuid().ToString("N"));
        private readonly StubHandler _handler = new StubHandler();
        private readonly OcrReaderDownloadService _service;

        public OcrReaderDownloadServiceTests()
        {
            Directory.CreateDirectory(_folder);

            OcrReader greek = new OcrReader { Id = "greek", Name = "Greek", ModelSha256 = Convert.ToHexStringLower(SHA256.HashData(MODEL)) };
            OcrReader latin = new OcrReader { Id = "latin", Name = "Latin", IsBuiltIn = true };
            _service = new OcrReaderDownloadService(new StubFactory(_handler), NullLogger<OcrReaderDownloadService>.Instance, _folder, [latin, greek]);
        }

        public void Dispose()
        {
            _service.Dispose();
            _handler.Dispose();
            Directory.Delete(_folder, recursive: true);
        }

        [Fact]
        public async Task A_reader_is_kept_with_its_character_list()
        {
            _handler.Files["ocr-reader-greek.onnx"] = MODEL;
            _handler.Files["ocr-reader-greek.yml"] = CHARACTERS;

            string? error = await _service.DownloadAsync("greek", Ct);

            error.ShouldBeNull();
            File.ReadAllBytes(Path.Combine(_folder, "ocr-reader-greek.onnx")).ShouldBe(MODEL);
            File.ReadAllBytes(Path.Combine(_folder, "ocr-reader-greek.yml")).ShouldBe(CHARACTERS);
            _handler.Requested.ShouldContain($"{OcrReaderCatalog.RELEASE_URL}/ocr-reader-greek.onnx");
        }

        [Fact]
        public async Task A_model_that_is_not_the_one_expected_is_not_kept()
        {
            _handler.Files["ocr-reader-greek.onnx"] = Encoding.ASCII.GetBytes("something else");
            _handler.Files["ocr-reader-greek.yml"] = CHARACTERS;

            string? error = await _service.DownloadAsync("greek", Ct);

            error.ShouldNotBeNull().ShouldContain("not the one expected");
            Directory.GetFiles(_folder).ShouldBeEmpty();
        }

        [Fact]
        public async Task A_reader_missing_from_the_release_says_where_it_looked()
        {
            string? error = await _service.DownloadAsync("greek", Ct);

            error.ShouldNotBeNull().ShouldContain(OcrReaderCatalog.RELEASE_URL);
            Directory.GetFiles(_folder).ShouldBeEmpty();
        }

        [Fact]
        public async Task The_reader_built_in_is_never_downloaded()
        {
            (await _service.DownloadAsync("latin", Ct)).ShouldBeNull();

            _handler.Requested.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_reader_the_app_does_not_know_is_refused()
        {
            (await _service.DownloadAsync("klingon", Ct)).ShouldNotBeNull();

            _handler.Requested.ShouldBeEmpty();
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed class StubHandler : HttpMessageHandler
        {
            public Dictionary<string, byte[]> Files { get; } = new Dictionary<string, byte[]>();
            public List<string> Requested { get; } = new List<string>();

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string url = request.RequestUri!.ToString();
                Requested.Add(url);

                if (!Files.TryGetValue(url[(url.LastIndexOf('/') + 1)..], out byte[]? bytes))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
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
