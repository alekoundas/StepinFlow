using Microsoft.Extensions.Logging;
using ProtoBuf;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipes;
using Transport.Ipc.Models;
using Transport.Ipc.Protobuf;

namespace Transport.Ipc
{
    /// <summary>
    /// Requests from Electron, taken in the order they came.
    /// </summary>
    public sealed class IpcRequestPipe
    {
        // Far above the largest request, every monitor's screenshot sent back to be cropped.
        private const int MAX_REQUEST_LENGTH = 256 * 1024 * 1024;

        private readonly IIpcDispatcher _ipcDispatcher;
        private readonly ILogger<IpcRequestPipe> _logger;

        public IpcRequestPipe(IIpcDispatcher ipcDispatcher, ILogger<IpcRequestPipe> logger)
        {
            _logger = logger;
            _ipcDispatcher = ipcDispatcher;
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
                    _logger.LogInformation("[.NET Pipe] Waiting for connection...");
                    await requestPipe.WaitForConnectionAsync(stoppingToken);
                    _logger.LogInformation("[.NET Pipe] Client connected.");

                    // Start listening for requests
                    await HandleRequestsAsync(requestPipe, stoppingToken);
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
        // Internal methods
        // ================================================================

        internal async Task HandleRequestsAsync(Stream pipe, CancellationToken ct)
        {
            // Use ArrayPool to reduce GC pressure on large images
            byte[] buffer = ArrayPool<byte>.Shared.Rent(128 * 1024);
            List<Task> sideBySide = new List<Task>();
            using SemaphoreSlim writing = new SemaphoreSlim(1, 1); // Make sure only 1 Task writes to the pipe.

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Read length prefix (4 bytes big-endian) ex. [00 00 01 F4][ 500 bytes: IpcRequest ][00 00 00 2A][ 42 bytes: IpcRequest ] ...
                    int bytesRead = await pipe.ReadAtLeastAsync(buffer.AsMemory(0, 4), 4, throwOnEndOfStream: false, ct);
                    if (bytesRead < 4) break;

                    int length = BinaryPrimitives.ReadInt32BigEndian(buffer);
                    if (length < 0 || length > MAX_REQUEST_LENGTH)
                    {
                        _logger.LogWarning("[.NET Pipe] A request said it was {Length} bytes long. The pipe is out of step, so the connection is closed.", length);
                        break;
                    }

                    // Resize buffer if needed
                    if (length > buffer.Length - 4)
                    {
                        // Rented first, so a Rent that throws leaves the old buffer for the finally to return once.
                        byte[] larger = ArrayPool<byte>.Shared.Rent(length + 64);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = larger;
                    }

                    // Read the data based on the length resolved above.
                    bytesRead = await pipe.ReadAtLeastAsync(buffer.AsMemory(0, length), length, throwOnEndOfStream: false, ct);
                    if (bytesRead != length) break;

                    // Deserialize request
                    IpcRequest request = Serializer.Deserialize<IpcRequest>(new ReadOnlyMemory<byte>(buffer, 0, length));

                    // Retrieve the actual handler.
                    IpcCall ipcCall = _ipcDispatcher.Prepare(request, ct);

                    // Await for blocking request to complete.
                    if (ipcCall.WaitsItsTurn)
                    {
                        await AnswerAsync(pipe, ipcCall, writing, ct);
                    }
                    
                    //Or add to the thread pool the side-by-side request.
                    else
                    {
                        sideBySide.RemoveAll(x => x.IsCompleted);
                        sideBySide.Add(Task.Run(() => AnswerAsync(pipe, ipcCall, writing, ct), CancellationToken.None));
                    }
                }
            }
            finally
            {
                // Answered before the services they use are disposed.
                await Task.WhenAll(sideBySide);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private async Task AnswerAsync(Stream pipe, IpcCall ipcCall, SemaphoreSlim writing, CancellationToken ct)
        {
            IpcResponse response = await ipcCall.Answer();

            using MemoryStream ms = new MemoryStream(32 * 1024);
            Serializer.Serialize(ms, response);
            byte[] responseBytes = ms.ToArray();

            try
            {
                // One answer at a time, or two would interleave their bytes.
                await writing.WaitAsync(ct);
                try
                {
                    byte[] prefix = new byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(prefix, responseBytes.Length);

                    await pipe.WriteAsync(prefix, ct);
                    await pipe.WriteAsync(responseBytes, ct);
                    await pipe.FlushAsync(ct);

                }
                finally
                {
                    writing.Release();
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
            {
                // Electron went away, or the backend is closing: nobody is left to answer.
                if (_logger.IsEnabled(LogLevel.Information))
                    _logger.LogInformation("[.NET Pipe] '{Action}' was not answered: {Reason}", response.Action, ex.Message);
            }
        }
    }
}
