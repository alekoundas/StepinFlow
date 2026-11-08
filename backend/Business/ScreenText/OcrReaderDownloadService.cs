using System.Net;
using System.Security.Cryptography;
using Core.Catalogs;
using Core.Models.Business.Ocr;
using Microsoft.Extensions.Logging;

namespace Business.ScreenText
{
    /// <summary>
    /// A text reader from the app's own release: its model, checked against the hash the catalog
    /// holds before it is kept, and its character list. Eight megabytes or so, so the caller waits
    /// for it rather than watching progress. One at a time.
    /// </summary>
    public sealed class OcrReaderDownloadService : IOcrReaderDownloadService, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OcrReaderDownloadService> _logger;
        private readonly string _folder;
        private readonly IReadOnlyList<OcrReader> _readers;
        private readonly SemaphoreSlim _oneAtATime = new SemaphoreSlim(1, 1);

        public OcrReaderDownloadService(IHttpClientFactory httpClientFactory, ILogger<OcrReaderDownloadService> logger, string folder, IReadOnlyList<OcrReader> readers)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _folder = folder;
            _readers = readers;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<string?> DownloadAsync(string language, CancellationToken ct = default)
        {
            OcrReader? reader  = _readers.FirstOrDefault(x => string.Equals(x.Id, language, StringComparison.OrdinalIgnoreCase));

            if (reader == null)
                return $"\"{language}\" is not a text reader this app knows.";

            if (reader.IsBuiltIn)
                return null;

            await _oneAtATime.WaitAsync(ct);
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(OcrReaderDownloadService));

                byte[] model = await client.GetByteArrayAsync($"{OcrReaderCatalog.RELEASE_URL}/{reader.ModelFileName}", ct);
                string hash = Convert.ToHexStringLower(SHA256.HashData(model));
                if (!string.Equals(hash, reader.ModelSha256, StringComparison.OrdinalIgnoreCase))
                    return $"The {reader.Name} reader that arrived is not the one expected, so it was not kept.";

                byte[] characters = await client.GetByteArrayAsync($"{OcrReaderCatalog.RELEASE_URL}/{reader.CharactersFileName}", ct);

                // The model first and the list last: a reader counts as downloaded once both are there.
                await WriteAsync(reader.ModelFileName, model, ct);
                await WriteAsync(reader.CharactersFileName, characters, ct);

                return null;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "The {Reader} text reader is not in the release.", reader.Id);
                return $"The {reader.Name} reader is not on the download page ({OcrReaderCatalog.RELEASE_URL}).";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Could not download the {Reader} text reader.", reader.Id);
                return $"Could not download the {reader.Name} reader. {ex.Message}";
            }
            finally
            {
                _oneAtATime.Release();
            }
        }

        public void Dispose()
        {
            _oneAtATime.Dispose();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Written aside and moved in, so a download cut short is never taken for the file.
        private async Task WriteAsync(string fileName, byte[] bytes, CancellationToken ct)
        {
            string path = Path.Combine(_folder, fileName);
            string part = path + ".part";

            await File.WriteAllBytesAsync(part, bytes, ct);
            File.Move(part, path, overwrite: true);
        }
    }
}
