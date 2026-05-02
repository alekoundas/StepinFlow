using Core.Models.Ipc.Protobuf;
using ProtoBuf;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipes;

namespace App.Ipc
{
    public sealed class IpcRequestPipe
    {
        private readonly IpcDispatcher _dispatcher;
        public IpcRequestPipe(IpcDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }


        // ================================================================
        // Background service
        // ================================================================
        public async Task StartBackgroundService(CancellationToken stoppingToken = default)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using NamedPipeServerStream requestPipe = new NamedPipeServerStream(
                    pipeName: "stepinflow-request",
                    direction: PipeDirection.InOut,
                    maxNumberOfServerInstances: 5,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                    inBufferSize: 64 * 1024,
                    outBufferSize: 64 * 1024
                );

                try
                {
                    // Wait for Electron to conect to the pipe.
                    Console.WriteLine("[.NET Pipe] Waiting for connection...");
                    await requestPipe.WaitForConnectionAsync(stoppingToken);
                    Console.WriteLine("[.NET Pipe] Client connected.");

                    // Start listening for requests
                    await HandleRequestAsync(requestPipe, stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[.NET Pipe] Error: {ex.Message}");
                }
                finally
                {
                    // Ensure pipe is properly closed
                    if (requestPipe.IsConnected)
                    {
                        try { requestPipe.Disconnect(); } catch { }
                    }
                }
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task HandleRequestAsync(NamedPipeServerStream pipe, CancellationToken ct)
        {
            // Use ArrayPool to reduce GC pressure on large images
            byte[] buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            try
            {
                while (!ct.IsCancellationRequested && pipe.IsConnected)
                {
                    // Read length prefix (4 bytes big-endian)
                    int bytesRead = await ReadExactAsync(pipe, buffer, 0, 4, ct);
                    if (bytesRead == 0) break;

                    int length = BinaryPrimitives.ReadInt32BigEndian(buffer);

                    // Resize buffer if needed
                    if (length > buffer.Length - 4)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = ArrayPool<byte>.Shared.Rent(length + 64);
                    }


                    bytesRead = await ReadExactAsync(pipe, buffer, 0, length, ct);
                    if (bytesRead != length) break;

                    // Deserialize request
                    IpcRequest request = Serializer.Deserialize<IpcRequest>(new ReadOnlyMemory<byte>(buffer, 0, length));

                    // Handle via dispatcher
                    IpcResponse response = await _dispatcher.HandleAsync(request, ct);


                    // Serialize response
                    using MemoryStream ms = new MemoryStream(32 * 1024);
                    Serializer.Serialize(ms, response);
                    byte[] responseBytes = ms.ToArray();

                    await WriteWithLengthPrefixAsync(pipe, responseBytes, ct);
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
        private static async Task WriteWithLengthPrefixAsync(Stream stream, byte[] data, CancellationToken ct)
        {
            var prefix = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(prefix, data.Length);

            await stream.WriteAsync(prefix, ct);
            await stream.WriteAsync(data, ct);
            await stream.FlushAsync(ct);
        }
    }
}
