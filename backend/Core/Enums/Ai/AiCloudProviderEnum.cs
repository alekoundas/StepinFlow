namespace Core.Enums
{
    /// <summary>
    /// A cloud model added on top of the local one. Ollama is not a choice here: it is always there,
    /// and every AI feature is off without it.
    /// </summary>
    public enum AiCloudProviderEnum
    {
        /// <summary>No cloud model. The local one answers everything.</summary>
        NONE,
        OPENAI,
    }
}
