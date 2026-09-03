using Microsoft.Extensions.AI;

namespace Business.Services.Ai.Providers
{
    /// <summary>
    /// Sets the two things Ollama gets wrong for this app by default.
    ///
    /// num_ctx, because Ollama serves 4096 tokens whatever the model can hold - a model advertising
    /// 256K still arrives at 4K - and the system prompt, eleven tool schemas and one screenshot do
    /// not fit in that. A num_ctx baked into a model's own Modelfile still wins over this, which is
    /// what the reported context in settings is for.
    ///
    /// think, because a thinking model spends its whole output budget reasoning and returns an
    /// empty answer. Measured on qwen3.5:4b: 5582 characters of reasoning, no answer at all, and
    /// with room to finish it took 10.3 seconds against 2.9 with thinking off, for the same answer.
    /// Harmless on a model that cannot think - it just ignores it.
    /// </summary>
    public sealed class OllamaContextChatClient : DelegatingChatClient
    {
        private readonly int _contextLength;

        public OllamaContextChatClient(IChatClient inner, int contextLength)
            : base(inner)
        {
            _contextLength = contextLength;
        }

        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return base.GetResponseAsync(messages, WithContextLength(options), cancellationToken);
        }

        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return base.GetStreamingResponseAsync(messages, WithContextLength(options), cancellationToken);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Cloned rather than edited: the caller owns what it passed, and the tool loop reuses it
        // across every round.
        private ChatOptions WithContextLength(ChatOptions? options)
        {
            ChatOptions cloned = options?.Clone() ?? new ChatOptions();

            cloned.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            cloned.AdditionalProperties["num_ctx"] = _contextLength;
            cloned.AdditionalProperties["think"] = false;

            return cloned;
        }
    }
}
