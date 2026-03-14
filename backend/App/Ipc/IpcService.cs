using Core.Models.Database;
using Core.Models.Ipc.Protobuf;
using ProtoBuf;
using System.Buffers;
using System.IO.Pipes;
using System.Text.Json;

namespace App.Ipc
{
    public class IpcService
    {
        private const string PipeName = "stepinflow-backend-pipe";

        //private readonly IpcDispatcher _dispatcher;

        //public IpcService(IpcDispatcher dispatcher)
        //{
        //    _dispatcher = dispatcher;
        //}

        public async Task StartAsync(CancellationToken stoppingToken = default)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 5,           
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                    inBufferSize: 64 * 1024,
                    outBufferSize: 64 * 1024
                );

                try
                {
                    Console.WriteLine("[.NET Pipe] Waiting for connection...");
                    await server.WaitForConnectionAsync(stoppingToken);
                    Console.WriteLine("[.NET Pipe] Client connected.");

                    await HandleConnectionAsync(server, stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[.NET Pipe] Error: {ex.Message}");
                }
            }
        }

        private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken ct)
        {
            // Use ArrayPool to reduce GC pressure on large images
            var buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            try
            {
                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    // Read length prefix (4 bytes big-endian)
                    int bytesRead = await ReadExactAsync(pipe, buffer, 0, 4, ct);
                    if (bytesRead == 0) break;

                    int length = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
                    if (length < 1 || length > 16 * 1024 * 1024) // safety limit ~16 MB
                    {
                        Console.Error.WriteLine($"[Pipe] Invalid length: {length}");
                        break;
                    }

                    // Resize buffer if needed
                    if (length > buffer.Length)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = ArrayPool<byte>.Shared.Rent(length + 16);
                    }

                    bytesRead = await ReadExactAsync(pipe, buffer, 0, length, ct);
                    if (bytesRead != length) break;

                    // Deserialize request
                    var request = Serializer.Deserialize<IpcRequest>(new ReadOnlyMemory<byte>(buffer, 0, length));

                    // Handle via dispatcher
                    //var response = await _dispatcher.HandleAsync(request, ct);

                    // TEMP TEST DUMMY RESPONSE
                    IpcResponse response;
                    if (request.Action == "greet")
                    {
                        var payloadObj = new { greeting = "Hello from .NET backend via protobuf IPC!" };
                        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payloadObj);

                        response = new IpcResponse
                        {
                            Action = request.Action,
                            CorrelationId = request.CorrelationId ?? "",
                            Payload = payloadBytes,
                            Error = ""
                        };
                        Console.WriteLine("[.NET Pipe] Handled 'greet' action");
                    }
                    else
                    {
                        var payloadObj = new { message = $"Unknown action: {request.Action}" };
                        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payloadObj);

                        response = new IpcResponse
                        {
                            Action = request.Action ?? "unknown",
                            CorrelationId = request.CorrelationId ?? "",
                            Payload = payloadBytes,
                            Error = $"Unknown action: {request.Action}"
                        };
                    }


                    // Serialize response
                    using var ms = new MemoryStream(32 * 1024);
                    Serializer.Serialize(ms, response);
                    var responseBytes = ms.ToArray();

                    // Write length prefix + payload
                    byte[] lenPrefix = new byte[4]
                    {
                        (byte)(responseBytes.Length >> 24),
                        (byte)(responseBytes.Length >> 16),
                        (byte)(responseBytes.Length >> 8),
                        (byte)responseBytes.Length
                    };

                    await pipe.WriteAsync(lenPrefix, 0, 4, ct);
                    await pipe.WriteAsync(responseBytes, 0, responseBytes.Length, ct);
                    await pipe.FlushAsync(ct);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset + total, count - total), ct);
                if (read == 0) return total;
                total += read;
            }
            return total;
        }
    }
}
