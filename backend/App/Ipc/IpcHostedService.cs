//using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace App.Ipc
{
    public class IpcHandler
    {
        public void StartListening()
        {

            Console.WriteLine("Yooooooooo");

            string? line;
            while ((line = Console.In.ReadLine()) != null)
            {
                try
                {
                    // Parse incoming JSON
                    var message = JsonSerializer.Deserialize<Message>(line.Trim());
                    if (message == null) continue;

                    // Process based on action (example)
                    object responsePayload;
                        Console.WriteLine(message.Action);
                    switch (message.Action)
                    {
                        case "greet":
                            responsePayload = new { Greeting = $"Hello, {message.Payload} from .NET!" };
                            break; ;
                        default:
                            responsePayload = new { Error = "Unknown action" };
                            break;
                    }

                    // Send JSON response to stdout
                    var response = new Message { Action = "response", Payload = responsePayload };
                    Console.WriteLine(JsonSerializer.Serialize(response));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        //public async Task StartListening2()
        //{
        //    // Start TCP server
        //    var listener = new TcpListener(IPAddress.Loopback, 5000);
        //    listener.Start();
        //    Console.WriteLine($"Listening on port {5000}...");

        //    while (true)
        //    {
        //        using var client = await listener.AcceptTcpClientAsync();
        //        using var stream = client.GetStream();

        //        // Handle bidirectional JSON communication (similar to your stdin/stdout)
        //        // Read from stream (incoming from Electron)
        //        var buffer = new byte[4096];
        //        int bytesRead;
        //        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        //        {
        //            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //            // Parse JSON, process (your existing logic), and respond
        //            //var response = ProcessMessage(message); // Your JSON handler function
        //            var responseBytes = Encoding.UTF8.GetBytes("response");
        //            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
        //        }
        //    }
        //}

    }

    // Shared message type (mirror in TS)
    public class Message
    {
        public string Action { get; set; } = string.Empty;
        public object Payload { get; set; } = new object();
    }
}