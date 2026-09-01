namespace Core.Enums
{
    /// <summary>
    /// Who runs the model. OLLAMA and OPENAI are the same client with a different address - Ollama
    /// serves an OpenAI compatible api - which is why local costs no extra provider code.
    /// </summary>
    public enum AiProviderEnum
    {
        /// <summary>No provider set. Every AI feature stays switched off rather than failing.</summary>
        NONE,
        OPENAI,
        OLLAMA,
    }
}
