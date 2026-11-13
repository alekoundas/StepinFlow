using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Business.Ai.Helpers;
using static Business.Tests.TestToken;

namespace Business.Tests.Ai
{
    /// <summary>
    /// How a connection to Ollama is opened, on real ports of this machine.
    /// Test:
    /// 1) localhost reaches a port listening on 127.0.0.1 without waiting on ::1 first
    /// 2) A local port nothing listens on is given up on quickly
    /// </summary>
    public sealed class OllamaConnectionHelperTests
    {
        // Windows takes two seconds to give up on each refused address, which is what both tests rule out.
        private static readonly TimeSpan QUICK_ENOUGH = TimeSpan.FromSeconds(1);

        [Fact]
        public async Task Localhost_reaches_a_port_on_IPv4_without_waiting_on_IPv6()
        {
            using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Stopwatch watch = Stopwatch.StartNew();
            await using Stream stream = await OllamaConnectionHelper.ConnectAsync(new DnsEndPoint("localhost", port), Ct);

            watch.Elapsed.ShouldBeLessThan(QUICK_ENOUGH);
        }

        [Fact]
        public async Task A_local_port_nothing_listens_on_is_given_up_on_quickly()
        {
            int port = FreePort();

            Stopwatch watch = Stopwatch.StartNew();
            await Should.ThrowAsync<HttpRequestException>(async () => await OllamaConnectionHelper.ConnectAsync(new DnsEndPoint("localhost", port), Ct));

            watch.Elapsed.ShouldBeLessThan(QUICK_ENOUGH);
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static int FreePort()
        {
            using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}
