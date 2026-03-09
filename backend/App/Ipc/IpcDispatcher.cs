using Core.Models.ProtobufIpcMessages;
using System.Text.Json;

namespace App.Ipc
{
    public class IpcDispatcher
    {
        // PropertyNamingPolicy: Use camelCase for JSON properties
        private JsonSerializerOptions _seralizeOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly Dictionary<string, IIpcService> _handlers;

        public IpcDispatcher(IEnumerable<IIpcService> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.Action, h => h);
        }

        public async Task HandleAsync(IpcRequest request, TextWriter writer)
        {
            if (!_handlers.TryGetValue(request.Action, out var handler))
            {
                await SendError(writer, request, $"Unknown action: {request.Action}");
                return;
            }

            try
            {
                var response = await handler.HandleAsync(request.Payload!, CancellationToken.None);
                await writer.WriteLineAsync(JsonSerializer.Serialize(response, _seralizeOptions));
            }
            catch (Exception ex)
            {
                await SendError(writer, request, ex.Message);
            }
        }

        private async Task SendError(TextWriter writer, IpcRequest req, string message)
        {
            var errorResponse = new IpcResponse() 
            { 
                Action = req.Action,
                Payload = null,
                CorrelationId = req.CorrelationId, 
                Error = message 
            };
            await writer.WriteLineAsync(JsonSerializer.Serialize(errorResponse, _seralizeOptions));
        }
    }
}