using System.Net;
using System.Net.Sockets;

namespace Business.Ai.Helpers
{
    /// <summary>
    /// How every client that talks to Ollama connects: IPv4 first, and a local address that does not
    /// answer is given up on at once.
    ///
    /// "localhost" resolves to ::1 before 127.0.0.1, Ollama listens on 127.0.0.1 only, and Windows
    /// takes two seconds to give up on a refused connection. Connecting the usual way cost two seconds
    /// for each new connection, and four for every call when Ollama was not there.
    /// </summary>
    public static class OllamaConnectionHelper
    {
        // A local port that is listening answers in well under a millisecond.
        private static readonly TimeSpan LOCAL_CONNECT_TIMEOUT = TimeSpan.FromMilliseconds(200);

        /// <summary>The handler for an HttpClient that talks to Ollama.</summary>
        public static SocketsHttpHandler CreateHandler()
        {
            SocketsHttpHandler handler = new SocketsHttpHandler();
            handler.ConnectCallback = (context, ct) => ConnectAsync(context.DnsEndPoint, ct);

            return handler;
        }

        /// <summary>Tries each address of the host, IPv4 ones first.</summary>
        public static async ValueTask<Stream> ConnectAsync(DnsEndPoint endPoint, CancellationToken ct)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(endPoint.Host, ct);
            Exception? lastError = null;

            foreach (IPAddress address in addresses.OrderBy(x => x.AddressFamily == AddressFamily.InterNetworkV6))
            {
                Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                NetworkStream? stream = null;

                try
                {
                    socket.NoDelay = true;
                    await ConnectWithinAsync(socket, address, endPoint.Port, ct);

                    stream = new NetworkStream(socket, ownsSocket: true);
                    return stream;
                }
                catch (Exception exception) when (!ct.IsCancellationRequested)
                {
                    lastError = exception;
                }
                finally
                {
                    if (stream == null)
                        socket.Dispose();
                }
            }

            throw new HttpRequestException($"Nothing answered at {endPoint.Host}:{endPoint.Port}.", lastError);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static async Task ConnectWithinAsync(Socket socket, IPAddress address, int port, CancellationToken ct)
        {
            if (!IPAddress.IsLoopback(address))
            {
                await socket.ConnectAsync(address, port, ct);
                return;
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(LOCAL_CONNECT_TIMEOUT);

            await socket.ConnectAsync(address, port, timeout.Token);
        }
    }
}
