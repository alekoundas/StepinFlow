using Microsoft.Extensions.AI;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// A model that answers what the test says, given what it was sent. A test that wants it
    /// unreachable throws from the answer. Every request is kept, as sent.
    /// </summary>
    public sealed class FakeChatClient : IChatClient
    {
        private readonly Func<IReadOnlyList<ChatMessage>, string> _answer;

        public FakeChatClient(Func<IReadOnlyList<ChatMessage>, string> answer)
        {
            _answer = answer;
        }

        public List<List<ChatMessage>> Requests { get; } = new List<List<ChatMessage>>();

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            List<ChatMessage> sent = messages.ToList();
            Requests.Add(sent);

            string text = _answer(sent);
            ChatResponse response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));

            return Task.FromResult(response);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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
