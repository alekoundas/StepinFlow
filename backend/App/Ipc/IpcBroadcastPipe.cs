
using Core.Models.Ipc.Protobuf;
using ProtoBuf;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Threading.Channels;

namespace App.Ipc
{
    public sealed class IpcBroadcastPipe
    {
        // Broadcast queue.
        //
        // Bounded on purpose: the reader only runs while a client is connected, so an unbounded
        // queue grows for as long as Electron is away and then replays thousands of obsolete
        // events at the next client. Broadcasts are live state, so the newest ones are the only
        // ones worth keeping.
        private readonly Channel<IpcBroadcast> _broadcastChannel = Channel.CreateBounded<IpcBroadcast>(
            new BoundedChannelOptions(1024)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
            });

        public IpcBroadcastPipe()
        {
        }


        // ================================================================
        // Background service
        // ================================================================
        public async Task StartBackgroundService(CancellationToken stoppingToken = default)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using NamedPipeServerStream broadcastPipe = new NamedPipeServerStream(
                    pipeName: "stepinflow-broadcast",
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
                    await broadcastPipe.WaitForConnectionAsync(stoppingToken);
                    Console.WriteLine("[.NET Pipe] Client connected.");

                    // Anything queued while nobody was listening is stale by definition.
                    while (_broadcastChannel.Reader.TryRead(out _)) { }

                    // Start handling broadcasts
                    await HandleBroadcastAsync(broadcastPipe, stoppingToken);

                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[.NET Pipe] Error: {ex.Message}");
                }
                finally
                {
                    // Ensure pipe is properly closed
                    if (broadcastPipe.IsConnected)
                    {
                        try { broadcastPipe.Disconnect(); } catch { }
                    }
                }
            }
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

        private async Task HandleBroadcastAsync(NamedPipeServerStream pipe, CancellationToken ct)
        {
            using var pipeCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Background task: cancel pipeCts when client disconnects
            _ = Task.Run(async () =>
            {
                while (!pipeCts.Token.IsCancellationRequested)
                {
                    if (!pipe.IsConnected) { pipeCts.Cancel(); break; }
                    await Task.Delay(500, pipeCts.Token).ConfigureAwait(false);
                }
            }, pipeCts.Token);

            await foreach (IpcBroadcast msg in _broadcastChannel.Reader.ReadAllAsync(pipeCts.Token))
            {
                try
                {
                    // No logging per message: console writes are synchronous, and during an
                    // overlay drag this runs 60 times a second.
                    using MemoryStream ms = new MemoryStream(32 * 1024);
                    Serializer.Serialize(ms, msg);
                    await WriteWithLengthPrefixAsync(pipe, ms.ToArray(), pipeCts.Token);
                }
                catch (Exception ex)
                {
                    // Normal when the client goes away: drop this pipe and let the outer loop
                    // wait for the next connection.
                    Console.WriteLine($"[.NET Pipe] Broadcast client gone: {ex.Message}");
                    pipeCts.Cancel();
                    break;
                }
            }
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
