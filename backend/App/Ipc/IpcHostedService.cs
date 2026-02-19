using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace App.Ipc
{
    public class IpcHandler
    {

        private JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true  // Enable case-insensitive matching
        };


        public async Task StartListening()
        {
            Console.WriteLine("Yooooooooo");
            await ProcessStreamAsync(Console.In, Console.Out);
        }


        public async Task StartListeningDebug()
        {
            string PipeName = "stepinflow-backend-pipe";
            while (true) // Loop for reconnections
            {
                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1))
                {
                    try
                    {
                        Console.WriteLine("[.NET] Waiting for Electron connection...");
                        await server.WaitForConnectionAsync();
                        Console.WriteLine("[.NET] Electron connected to pipe.");
                        using (var reader = new StreamReader(server, Encoding.UTF8, true, 4096, true))
                        using (var writer = new StreamWriter(server, Encoding.UTF8, 4096, true) { AutoFlush = true })
                        {
                            await ProcessStreamAsync(reader, writer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[.NET] Pipe error: {ex.Message}");
                    }
                }
            }
        }


        private async Task ProcessStreamAsync(TextReader reader, TextWriter writer)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    Console.WriteLine($"[.NET] Received raw line: {line}"); // Debug raw input

                    Message? message = JsonSerializer.Deserialize<Message>(line.Trim(), options); // Parse incoming JSON
                    if (message == null) continue;
                    Console.WriteLine($"[.NET] Parsed action: {message.Action}");

                    // Process based on action
                    object responsePayload;
                    switch (message.Action)
                    {
                        case "greet":
                            responsePayload = new { Greeting = $"Hello, {message.Payload} from .NET!" };
                            break;
                        case "test":
                            responsePayload = new { TestResponse = "Test received!" };
                            break;
                        default:
                            responsePayload = new { Error = "Unknown action" };
                            break;
                    }

                    // Send JSON response
                    Message response = new Message { Action = "d", Payload = responsePayload };
                    string responseJson = JsonSerializer.Serialize(response);
                    await writer.WriteLineAsync(responseJson);
                    Console.WriteLine($"[.NET]Sent response: {responseJson}"); // Debug send
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[.NET]Error processing message: {ex.Message}");
                }
            }
        }
    }

    // Shared message type (mirror in TS)
    public class Message
    {
        public string Action { get; set; } = string.Empty;
        public object Payload { get; set; } = new object();
    }
}