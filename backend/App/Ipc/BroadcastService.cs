using Core.Enums;
using Core.Interfaces;
using Core.Models.Ipc.Protobuf;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace App.Ipc
{
    public sealed class IpcBroadcastService : IIpcBroadcastService
    {
        private readonly IpcBroadcastPipe _ipcBroadcastPipe;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            //PropertyNameCaseInsensitive = true, // JS -> .Net
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // .Net -> JS
            ReferenceHandler = ReferenceHandler.IgnoreCycles // Ignore circular objects

        };

        public IpcBroadcastService(IpcBroadcastPipe ipcBroadcastPipe)
        {
            _ipcBroadcastPipe = ipcBroadcastPipe;

            // Add Enum to string converter
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }


        // ================================================================
        // Public methods
        // ================================================================
        public ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload)
        {
            byte[] payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, _jsonOptions);

            IpcBroadcast message = new IpcBroadcast
            {
                Type = type.ToString(), // TODO change to typed
                Payload = payloadBytes
            };
            return _ipcBroadcastPipe.BroadcastAsync(message);
        }
    }
}
