namespace Core.Models.Dtos
{
    /// <summary>
    /// What the chosen provider can be asked for. Empty with an Error when the provider cannot be
    /// reached, which for Ollama usually means it is not running.
    /// </summary>
    public class AiModelsDto
    {
        public IReadOnlyList<AiModelDto> Models { get; set; } = [];
        public string Error { get; set; } = string.Empty;
    }
}
