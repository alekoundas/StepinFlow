//using Newtonsoft.Json;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
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


        public void StartListening()
        {

            Console.WriteLine("Yooooooooo");

            string? line;
            while ((line = Console.In.ReadLine()) != null)
            {
                try
                {
                    // Parse incoming JSON
                    var message = JsonSerializer.Deserialize<Message>(line.Trim(), options);
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

        public async Task StartListening2()
        {
            string PipeName = "stepinflow-backend-pipe";
            while (true) // Loop for reconnections
            {
                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1))
                {
                    try
                    {
                        Console.WriteLine("Waiting for Electron connection...");
                        await server.WaitForConnectionAsync();
                        Console.WriteLine("Electron connected to pipe.");
                        using (var reader = new StreamReader(server, Encoding.UTF8, true, 4096, true))
                        using (var writer = new StreamWriter(server, Encoding.UTF8, 4096, true) { AutoFlush = true })
                        {
                            string? line;
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                try
                                {
                                    Console.WriteLine($"Received raw line: {line}"); // Debug raw input

                                    // Parse incoming JSON
                                    var message = JsonSerializer.Deserialize<Message>(line.Trim(), options);
                                    if (message == null) continue;

                                    Console.WriteLine($"Parsed action: {message.Action}");

                                    // Process based on action (your logic)
                                    object responsePayload;
                                    switch (message.Action)
                                    {
                                        case "greet":
                                            responsePayload = new { Greeting = $"Hello, {message.Payload} from .NET!" };
                                            break;
                                        case "test": // For test send
                                            responsePayload = new { TestResponse = "Test received!" };
                                            break;
                                        default:
                                            responsePayload = new { Error = "Unknown action" };
                                            break;
                                    }

                                    // Send JSON response
                                    var response = new Message { Action = "response", Payload = responsePayload };
                                    var responseJson = JsonSerializer.Serialize(response);
                                    await writer.WriteLineAsync(responseJson);
                                    Console.WriteLine($"Sent response: {responseJson}"); // Debug send
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine($"Error processing message: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Pipe error: {ex.Message}");
                    }
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