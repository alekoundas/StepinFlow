using System.Collections.Concurrent;
using Core.Enums;
using Core.Models.Dtos;
using Core.Ports;
using Microsoft.Extensions.Logging;

namespace Business.Ai
{
    /// <summary>
    /// The answers being written. An answer takes longer than a request can wait, on a pipe that
    /// answers one call at a time, so the request starts it and AI_CHAT_EVENT says how it goes.
    /// Several conversations can be answering at once; Ollama queues them.
    /// </summary>
    public sealed class AiChatSessionService : IAiChatSessionService
    {
        private readonly IFlowQuestionService _flowQuestionService;
        private readonly IIpcBroadcastService _broadcastService;
        private readonly ILogger<AiChatSessionService> _logger;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _answers = new ConcurrentDictionary<string, CancellationTokenSource>();

        public AiChatSessionService(
            IFlowQuestionService flowQuestionService,
            IIpcBroadcastService broadcastService,
            ILogger<AiChatSessionService> logger)
        {
            _flowQuestionService = flowQuestionService;
            _broadcastService = broadcastService;
            _logger = logger;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public bool Start(AiChatRequestDto request)
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            if (!_answers.TryAdd(request.AnswerId, cts))
                return false;

            _ = Task.Run(() => AnswerToEndAsync(request, cts), CancellationToken.None);

            return true;
        }

        public void Stop(string answerId)
        {
            if (_answers.TryGetValue(answerId, out CancellationTokenSource? cts))
                cts.Cancel();
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Fire and forget: nothing awaits this, so it must never throw.
        private async Task AnswerToEndAsync(AiChatRequestDto request, CancellationTokenSource cts)
        {
            try
            {
                await _flowQuestionService.AskAsync(request, Report, cts.Token);
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested)
                {
                    Report(new AiChatEventDto { AnswerId = request.AnswerId, Type = AiChatEventTypeEnum.STOPPED });
                }
                else
                {
                    _logger.LogWarning(ex, "The model could not be reached.");

                    Report(new AiChatEventDto
                    {
                        AnswerId = request.AnswerId,
                        Type = AiChatEventTypeEnum.FAILED,
                        Error = "The model could not be reached.",
                        ErrorDetail = ex.Message,
                    });
                }
            }
            finally
            {
                _answers.TryRemove(request.AnswerId, out _);
            }
        }

        private void Report(AiChatEventDto chatEvent)
        {
            _ = _broadcastService.SendAsync(BroadcastTypeEnum.AI_CHAT_EVENT, chatEvent).AsTask();
        }
    }
}
