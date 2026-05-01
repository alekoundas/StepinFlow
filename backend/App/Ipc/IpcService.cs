using Core.Models.Ipc.Protobuf;
using ProtoBuf;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Threading.Channels;

namespace App.Ipc
{
    public sealed class IpcService
    {
        // IPC pipe names
        private const string RequestPipeName = "stepinflow-request";
        private const string BroadcastPipeName = "stepinflow-broadcast";

        // Breadcast queue 
        private readonly Channel<IpcBroadcast> _broadcastChannel = Channel.CreateUnbounded<IpcBroadcast>();


        private readonly IpcDispatcher _dispatcher;
        public IpcService(IpcDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }


        // ================================================================
        // Background service
        // ================================================================
        public async Task StartBackgroundService(CancellationToken stoppingToken)
        {
            Task requestTask = StartRequestPipeAsync(stoppingToken);
            Task broadcastTask = StartBroadcastPipeAsync(stoppingToken);

            // If one pipe disconnects dont kill the other.
            await Task.WhenAll(requestTask, broadcastTask);
        }
      

        // ================================================================
        // Public methods
        // ================================================================
        public async ValueTask BroadcastAsync(IpcBroadcast ipcBroadcast)
        {
            await _broadcastChannel.Writer.WriteAsync(ipcBroadcast);
        }

        // ================================================================
        // Private methods
        // ================================================================

        private async Task StartRequestPipeAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await using NamedPipeServerStream pipe = new NamedPipeServerStream(
                  RequestPipeName,
                  PipeDirection.InOut,
                  maxNumberOfServerInstances: 5,
                  transmissionMode: PipeTransmissionMode.Byte,
                  options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                  inBufferSize: 64 * 1024,
                  outBufferSize: 64 * 1024
              );

                try
                {
                    await pipe.WaitForConnectionAsync(ct);
                    await HandleRequestAsync(pipe, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    Console.Error.WriteLine($"[.NET Pipe execption]: {ex}");
                }
            }
        }

        private async Task StartBroadcastPipeAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await using NamedPipeServerStream pipe = new NamedPipeServerStream(
                  BroadcastPipeName,
                  PipeDirection.Out,
                  maxNumberOfServerInstances: 5,
                  transmissionMode: PipeTransmissionMode.Byte,
                  options: PipeOptions.Asynchronous | PipeOptions.WriteThrough
              );

                try
                {
                    await pipe.WaitForConnectionAsync(ct);
                    await HandleBroadcastAsync(pipe, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    Console.Error.WriteLine($"[.NET Pipe execption]: {ex}");
                }
            }
        }


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


        private async Task HandleBroadcastAsync(NamedPipeServerStream pipe, CancellationToken ct)
        {
            await foreach (IpcBroadcast ipcBroadcast in _broadcastChannel.Reader.ReadAllAsync(ct))
            {
                    Console.WriteLine($"[.NET Broadcast]: {ipcBroadcast.Type.ToString()}");
                try
                {
                    using MemoryStream ms = new MemoryStream(32 * 1024);
                    Serializer.Serialize(ms, ipcBroadcast);
                    byte[] data = ms.ToArray();

                    await WriteWithLengthPrefixAsync(pipe, data, ct);
                }
                catch when (!ct.IsCancellationRequested)
                {
                    break; // Client disconnected
                }
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
