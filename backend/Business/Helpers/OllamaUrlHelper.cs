namespace Business.Helpers
{
    /// <summary>
    /// Ollama serves two things off one address: an OpenAI compatible api under /v1, and its own
    /// api under /api. The setting holds the base so neither path is baked into what the user typed.
    /// </summary>
    public static class OllamaUrlHelper
    {
        public static string ToOpenAiEndpoint(string baseUrl)
        {
            return $"{Trim(baseUrl)}/v1";
        }

        /// <summary>The models actually pulled onto this machine.</summary>
        public static string ToTagsEndpoint(string baseUrl)
        {
            return $"{Trim(baseUrl)}/api/tags";
        }

        /// <summary>Downloads one. Answers with a stream of progress lines, not a single reply.</summary>
        public static string ToPullEndpoint(string baseUrl)
        {
            return $"{Trim(baseUrl)}/api/pull";
        }

        /// <summary>
        /// A model name with its tag spelled out, so two of them can be compared.
        ///
        /// The tag is the model: qwen2.5:3b and qwen2.5:7b are different downloads of different
        /// sizes, and comparing only the part before the colon would call one of them the other.
        /// A name with no tag means :latest, which is what Ollama pulls when you leave it off.
        /// </summary>
        public static string NormaliseModelName(string name)
        {
            string trimmed = name.Trim();

            if (trimmed.Contains(':'))
                return trimmed.ToLowerInvariant();

            return $"{trimmed}:latest".ToLowerInvariant();
        }


        // ================================================================
        // Private methods
        // ================================================================

        /// <summary>
        /// A trailing slash, or a /v1 left over from an older default, would otherwise end up
        /// doubled in the middle of the path.
        /// </summary>
        private static string Trim(string baseUrl)
        {
            string trimmed = baseUrl.Trim().TrimEnd('/');

            if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[..^3].TrimEnd('/');

            return trimmed;
        }
    }
}
