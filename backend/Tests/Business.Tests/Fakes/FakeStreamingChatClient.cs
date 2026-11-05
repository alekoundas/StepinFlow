using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A model that streams what the test scripted, one round per request: the tool loop asks again
    /// after every tool it called. Every request is kept, as sent.
    /// </summary>
    public sealed class FakeStreamingChatClient : IChatClient
    {
        private readonly Queue<IReadOnlyList<AIContent>> _rounds;

        public FakeStreamingChatClient(params IReadOnlyList<AIContent>[] rounds)
        {
            _rounds = new Queue<IReadOnlyList<AIContent>>(rounds);
        }

        public List<List<ChatMessage>> Requests { get; } = new List<List<ChatMessage>>();

        /// <summary>Runs between one piece of a round and the next, for a test that stops it part way.</summary>
        public Action<AIContent>? OnContent { get; set; }

        /// <summary>Throws from the stream, as a model that cannot be reached does.</summary>
        public Exception? Failure { get; set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Requests.Add(messages.ToList());

            if (Failure != null)
                throw Failure;

            if (!_rounds.TryDequeue(out IReadOnlyList<AIContent>? round))
                yield break;

            foreach (AIContent content in round)
            {
                // Ollama's stream just ends when it is cancelled, rather than throwing.
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return new ChatResponseUpdate(ChatRole.Assistant, [content]);

                OnContent?.Invoke(content);
                await Task.Yield();
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
