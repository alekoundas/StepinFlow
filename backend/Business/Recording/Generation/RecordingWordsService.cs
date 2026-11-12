using System.Text.Json;
using Business.Ai.Helpers;
using Business.Ai.Providers;
using Business.Recording.Generation.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Business.Recording.Generation
{
    /// <summary>
    /// A model asked for the words of a recording's draft, never its script: a name, a comment and
    /// what a failed wait says, for each search by its id, and for a QA test where its stages begin
    /// and what to check. A small model writes a good name for one step; it does not rewrite a
    /// hundred lines with every reference intact, which is where the answers that wrote the whole
    /// script failed. So the answer is small, its shape is held by a JSON schema Ollama decodes
    /// against - an id can only be one the outline has - and the code puts it on the steps field
    /// by field (RecordingWordsApplier), where a bad field costs that field and nothing else.
    ///
    /// The flow format is not sent: a model that writes no script needs none of it, and the
    /// request is a few thousand tokens rather than fifteen.
    /// </summary>
    public sealed class RecordingWordsService : IRecordingWordsService
    {
        public const int ATTEMPT_COUNT = 2;

        // As many as a tester reads, and no more than a small model keeps track of.
        private const int MAXIMUM_STAGES = 4;
        private const int MAXIMUM_CHECKS = 3;
        private const int MAXIMUM_QUESTIONS = 3;
        private const int MAXIMUM_NOTES = 3;

        private static readonly JsonSerializerOptions JSON = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly IAiClientFactory _clientFactory;
        private readonly ILogger<RecordingWordsService> _logger;

        public RecordingWordsService(IAiClientFactory clientFactory, ILogger<RecordingWordsService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public string RequestOf(RecordingOutline outline, string example)
        {
            if (example.Length == 0)
                return outline.Text;

            return $"{outline.Text}\n\n{example}";
        }

        public Task<RecordingWordsResult> NameAsync(RecordingOutline outline, string request, bool isReadTextSent, Action<int>? onAttempt, CancellationToken ct = default)
        {
            string instructions = AiPromptHelper.NameRecordedSteps;
            if (!isReadTextSent)
                instructions += $"\n\n{AiPromptHelper.NamesArePlaceholders}";

            return AskAsync(instructions, request, NamesSchema(outline), onAttempt, ct);
        }

        public Task<RecordingWordsResult> ProposeTestAsync(RecordingOutline outline, bool isReadTextSent, Action<int>? onAttempt, CancellationToken ct = default)
        {
            return AskAsync(AiPromptHelper.ProposeTestParts, outline.Text, TestSchema(outline, isReadTextSent), onAttempt, ct);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task<RecordingWordsResult> AskAsync(string instructions, string request, JsonElement schema, Action<int>? onAttempt, CancellationToken ct)
        {
            RecordingWordsResult result = new RecordingWordsResult { Request = request };

            // Built per call from the settings, so it is ours to dispose.
            using IChatClient? chatClient = await _clientFactory.CreateAsync(ct);
            if (chatClient == null)
            {
                result.ErrorMessage = AiProviderService.NOT_SET_UP;
                return result;
            }

            List<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.System, instructions),
                new ChatMessage(ChatRole.User, request),
            ];

            ChatOptions options = new ChatOptions
            {
                Temperature = 0,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(schema, "words", "The words of a recording's draft."),
            };

            for (int attempt = 1; attempt <= ATTEMPT_COUNT; attempt++)
            {
                onAttempt?.Invoke(attempt);
                result.Attempts = attempt;

                ChatResponse response;
                try
                {
                    response = await chatClient.GetResponseAsync(messages, options, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning(ex, "The model could not write the words of the draft.");
                    result.ErrorMessage = $"The model could not be reached. {ex.Message}";
                    return result;
                }

                RecordingWords? words = Read(response.Text);
                if (words != null)
                {
                    result.Words = words;
                    return result;
                }
            }

            result.ErrorMessage = $"No answer from the model read as the JSON asked for in {ATTEMPT_COUNT} attempts.";
            return result;
        }

        // A model that wraps its JSON in a fence or a sentence still answered.
        private static RecordingWords? Read(string answer)
        {
            int start = answer.IndexOf('{');
            int end = answer.LastIndexOf('}');
            if (start < 0 || end <= start)
                return null;

            try
            {
                return JsonSerializer.Deserialize<RecordingWords>(answer[start..(end + 1)], JSON);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static JsonElement NamesSchema(RecordingOutline outline)
        {
            string[] ids = outline.Searches.Keys.ToArray();

            object step = Strict(new Dictionary<string, object>
            {
                ["id"] = new { type = "string", @enum = ids },
                ["name"] = new { type = "string" },
                ["comment"] = new { type = "string" },
                ["notFound"] = new { type = "string" },
            });

            return JsonSerializer.SerializeToElement(Strict(new Dictionary<string, object>
            {
                ["steps"] = new { type = "array", items = step },
                ["questions"] = Texts(MAXIMUM_QUESTIONS),
                ["notes"] = Texts(MAXIMUM_NOTES),
            }));
        }

        // Without what was read off the screen there are no words to check for, only stages.
        private static JsonElement TestSchema(RecordingOutline outline, bool isReadTextSent)
        {
            string[] ids = outline.Searches.Keys.ToArray();

            object stage = Strict(new Dictionary<string, object>
            {
                ["before"] = new { type = "string", @enum = ids },
                ["name"] = new { type = "string" },
                ["comment"] = new { type = "string" },
            });

            object check = Strict(new Dictionary<string, object>
            {
                ["after"] = new { type = "string", @enum = ids },
                ["words"] = new { type = "string" },
                ["name"] = new { type = "string" },
                ["comment"] = new { type = "string" },
                ["notFound"] = new { type = "string" },
            });

            int checks = isReadTextSent ? MAXIMUM_CHECKS : 0;

            return JsonSerializer.SerializeToElement(Strict(new Dictionary<string, object>
            {
                ["stages"] = new { type = "array", items = stage, maxItems = MAXIMUM_STAGES },
                ["checks"] = new { type = "array", items = check, maxItems = checks },
                ["questions"] = Texts(MAXIMUM_QUESTIONS),
                ["notes"] = Texts(MAXIMUM_NOTES),
            }));
        }

        private static object Texts(int maximum)
        {
            return new { type = "array", items = new { type = "string" }, maxItems = maximum };
        }

        // Every property required and no other, so the decoder writes each one and nothing else.
        private static object Strict(Dictionary<string, object> properties)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = properties.Keys.ToArray(),
                ["additionalProperties"] = false,
            };
        }
    }
}
