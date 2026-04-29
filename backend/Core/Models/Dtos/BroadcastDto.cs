using Core.Enums;

namespace Core.Models.Dtos
{
    public class BroadcastDto
    {
        public BroadcastTypeEnum Type { get; set; }
        public object? Data { get; private set; }
        public string? Message { get; private set; }
    }
}
