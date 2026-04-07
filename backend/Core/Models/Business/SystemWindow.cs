namespace Core.Models.Dtos
{
    public class SystemWindow
    {
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
    }
}
