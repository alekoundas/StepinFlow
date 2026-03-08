namespace Core.Models.Ipc
{
    public class IpcResponse
    {
        public string Action { get; set; } = string.Empty;
        public object? Payload { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string? Error { get; set; } 
    }
}


