namespace Core.Models.Dtos
{
    /// <summary>
    /// What can be chosen: the models downloaded into Ollama, and the cloud provider's. Models is
    /// empty with an Error when Ollama cannot be reached, which usually means it is not running.
    /// </summary>
    public class AiModelsDto
    {
        public IReadOnlyList<AiModelDto> Models { get; set; } = [];
        public IReadOnlyList<AiModelDto> CloudModels { get; set; } = [];
        public string Error { get; set; } = string.Empty;
    }
}
