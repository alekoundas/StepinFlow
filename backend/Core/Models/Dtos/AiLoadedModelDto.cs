namespace Core.Models.Dtos
{
    /// <summary>A model Ollama holds in memory, and how much of it that takes.</summary>
    public class AiLoadedModelDto
    {
        public string Name { get; set; } = string.Empty;
        public long MemoryBytes { get; set; }
    }
}
