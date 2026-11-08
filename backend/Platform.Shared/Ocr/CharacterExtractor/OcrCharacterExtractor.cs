using System.Text.Json;

namespace Platform.Shared.Ocr.CharacterExtractor
{
    /// <summary>
    /// Extract the characters a reader tells apart, from its inference.yml.
    /// The model answers with characters of: "no character here" + each entry of the character_dict in order, and a space last. 
    /// </summary>
    internal static class OcrCharacterExtractor
    {
        /// <summary>
        /// Read the model .yml file and extract the characters (classes) ordered.
        /// </summary>
        public static string[] Load(string ymlPath)
        {
            string fileName = Path.GetFileName(ymlPath);
            string[] lines = File.ReadAllLines(ymlPath);

            // "character_dict" contains all the characters (aka Classes) the model output can use - ordered.
            int start = Array.FindIndex(lines, x => x.TrimStart().StartsWith("character_dict:", StringComparison.Ordinal));
            if (start < 0)
                throw new InvalidDataException($"{fileName} has no character_dict.");

            // ex.   - '9'   or   - Õ
            IEnumerable<string> characters = lines
                .Skip(start + 1)
                .Select(x => x.TrimStart())
                .TakeWhile(x => x.StartsWith('-'))
                .Select(x => CharacterOf(x, fileName));

            // First place expects a "no character here" and a space last.
            return ["", .. characters, " "];
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string CharacterOf(string entry, string fileName)
        {
            if (entry.Length <= 2)
                throw new InvalidDataException($"{fileName} has an empty entry in its character_dict.");

            // Remove "- " from the start.
            string value = entry[2..];

            // Remove ' from start and end of value.
            if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
                return value[1..^1].Replace("''", "'", StringComparison.Ordinal);

            // Remove " from start and end of value and handle escaped chars. ex "\u200B".
            // JSON reads the escapes it shares with YAML; one only YAML has, like \x41, is refused.
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                try
                {
                    return JsonSerializer.Deserialize<string>(value) ?? value;
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException($"{fileName} has an escape in its character_dict this app cannot read: {value}", ex);
                }
            }

            return value;
        }
    }
}
