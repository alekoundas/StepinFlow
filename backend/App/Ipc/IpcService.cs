using Core.Models.ProtobufIpcMessages;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace App.Ipc
{
    public class IpcService
    {
        // PropertyNameCaseInsensitive: Enable case-insensitive matching
        private JsonSerializerOptions _deseralizeOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // PropertyNamingPolicy: Use camelCase for JSON properties
        private JsonSerializerOptions _seralizeOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


        public IpcService()
        {
        }


        public async Task StartListening()
        {
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
                    Console.WriteLine($"[.NET] Received raw line: {line}");
                    IpcRequest? message = JsonSerializer.Deserialize<IpcRequest>(line.Trim(), _deseralizeOptions);

                    if (message == null) continue;
                    Console.WriteLine($"[.NET] Parsed action: {message.Action}");

                    // Process based on action
                    switch (message.Action)
                    {
                        case "greet":
                            message.Payload = new { Greeting = $"Hello, {message.Payload} from .NET!" };
                            break;
                        case "test":
                            message.Payload = new { TestResponse = "Test received!" };
                            break;
                        default:
                            message.Payload = new { Error = "Unknown action" };
                            break;
                    }

                    // Send JSON response
                    string responseJson = JsonSerializer.Serialize(message, _seralizeOptions);
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
}