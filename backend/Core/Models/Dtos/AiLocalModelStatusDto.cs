using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>The local model, which every AI feature needs whatever cloud model is set.</summary>
    public class AiLocalModelStatusDto
    {
        public AiLocalModelStateEnum State { get; set; }
        public string Model { get; set; } = string.Empty;

        /// <summary>Zero unless it is in memory.</summary>
        public long MemoryBytes { get; set; }

        /// <summary>The cloud model that answers in the local model's place. Empty when none does.</summary>
        public string CloudModel { get; set; } = string.Empty;
    }
}
