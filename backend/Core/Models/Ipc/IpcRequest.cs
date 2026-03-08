namespace Core.Models.Ipc
{
    public class IpcRequest
    {
        public string Action { get; set; } = string.Empty;
        public object Payload { get; set; } = new object();
        public string CorrelationId { get; set; } = string.Empty;
    }
}
