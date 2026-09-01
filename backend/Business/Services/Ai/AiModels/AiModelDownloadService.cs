using System.Net.Http.Json;
using System.Text.Json;
using Business.Services.Ai.Helpers;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace Business.Services.Ai.AiModels
{
    /// <summary>
    /// Downloads a model, and remembers how it is going.
    /// One at a time.
    /// </summary>
    public sealed class AiModelDownloadService : IAiModelDownloadService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly ILogger<AiModelDownloadService> _logger;

        private readonly object _lockObj = new object();

        private AiModelDownloadEventDto? _current;

        public AiModelDownloadService(
            IHttpClientFactory httpClientFactory,
            IIpcBroadcastService broadcastService,
            ILogger<AiModelDownloadService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _broadcastService = broadcastService;
            _logger = logger;
        }

        /// <summary>
        /// Read under the lock. The download writes this from its own
        /// thread while a page reads it from the pipe's, and the "one at a time" guard is only worth
        /// anything if the value it tests is the one that was last written.
        /// </summary>
        public AiModelDownloadEventDto? Current
        {
            get
            {
                lock (_lockObj)
                {
                    return _current;
                }
            }
        }


        // ================================================================
        // Public methods
        // ================================================================

        public bool Start(string model, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(model))
                return false;

            lock (_lockObj)
            {
                if (_current != null && !_current.IsDone)
                    return false;

                _current = new AiModelDownloadEventDto
                {
                    Model = model,
                    Status = "starting",
                };
            }

            // Fire and forget: nothing awaits this, so it must never throw.
            _ = DownloadToEndAsync(model, baseUrl);

            return true;
        }

        /// <summary>
        /// Only a failure ever needs this. A finished download clears itself, because the model
        /// appearing in the list says it worked better than a banner would.
        /// </summary>
        public void Clear()
        {
            lock (_lockObj)
            {
                // A running download is not something a page can dismiss.
                if (_current != null && !_current.IsDone)
                    return;

                _current = null;
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Ollama answers a pull with a stream of json lines, one per progress update, for as long as the download takes.
        private async Task DownloadToEndAsync(string model, string baseUrl)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(nameof(AiModelDownloadService));
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, OllamaUrlHelper.ToDownloadEndpoint(baseUrl))
                {
                    Content = JsonContent.Create(new { model = model, stream = true }),
                };

                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using Stream stream = await response.Content.ReadAsStreamAsync();
                using StreamReader reader = new StreamReader(stream);

                while (true)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    OllamaDownloadProgress? progress = JsonSerializer.Deserialize<OllamaDownloadProgress>(line, _jsonOptions);
                    if (progress == null)
                        continue;

                    // Ollama answers a bad model name with 200 and an error line, then closes. That
                    // line is the end of the download and carries the only useful sentence in it -
                    // "pull model manifest: file does not exist" says what went wrong, where the
                    // generic message below could only say that something did.
                    bool isFailed = !string.IsNullOrWhiteSpace(progress.Error);

                    await ReportAsync(new AiModelDownloadEventDto
                    {
                        Model = model,
                        Status = progress.Status,
                        Completed = progress.Completed,
                        Total = progress.Total,
                        IsDone = isFailed || string.Equals(progress.Status, "success", StringComparison.OrdinalIgnoreCase),
                        Error = progress.Error,
                    });

                    if (isFailed)
                        return;
                }

                // The stream ended without saying "success", which Ollama does on some errors.
                AiModelDownloadEventDto? last = Current;

                if (last != null && !last.IsDone)
                {
                    await ReportAsync(new AiModelDownloadEventDto
                    {
                        Model = model,
                        IsDone = true,
                        Error = "The download ended without finishing. Check Ollama and try again.",
                    });

                    return;
                }

                // Nothing left to say. The model turning up in the list is the news, so a banner
                // saying it worked would only be something else to close.
                lock (_lockObj)
                {
                    _current = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not download the model {Model}.", model);

                await ReportAsync(new AiModelDownloadEventDto
                {
                    Model = model,
                    IsDone = true,
                    Error = $"The download stopped. {ex.Message}",
                });
            }
        }

        // Kept for whoever asks later, and sent to whoever is watching now.
        private async Task ReportAsync(AiModelDownloadEventDto payload)
        {
            lock (_lockObj)
            {
                _current = payload;
            }

            await _broadcastService.SendAsync(BroadcastTypeEnum.AI_MODEL_DOWNLOAD_EVENT, payload);
        }


        // ================================================================
        // Private types
        // ================================================================

        private sealed class OllamaDownloadProgress
        {
            public string Status { get; set; } = string.Empty;
            public long Completed { get; set; }
            public long Total { get; set; }
            public string Error { get; set; } = string.Empty;
        }
    }
}
