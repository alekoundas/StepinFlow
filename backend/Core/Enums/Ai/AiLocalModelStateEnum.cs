namespace Core.Enums
{
    /// <summary>Where the local model chosen in Settings stands, as Ollama says it right now.</summary>
    public enum AiLocalModelStateEnum
    {
        NOT_ANSWERING,
        NOT_CHOSEN,
        NOT_DOWNLOADED,
        NOT_IN_MEMORY,
        IN_MEMORY,
    }
}
