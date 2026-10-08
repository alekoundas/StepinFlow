using System.Security.Cryptography;

namespace Core.Helpers
{
    /// <summary>
    /// A file name Windows, macOS and Linux will accept.
    /// </summary>
    public static class FileNameHelper
    {
        private const string TemplateNameCharacters = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static readonly char[] InvalidCharacters = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

        // RESERVED and cannot be used! (Devices on Windows).
        private static readonly HashSet<string> ReservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³",
        };


        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Why the name cannot be a file name, or null when it can. 
        /// </summary>
        public static string? Validate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "The name is empty.";

            if (name.Any(IsInvalid))
                return "The name becomes a file name, so it can't contain < > : \" / \\ | ? * or control characters.";

            if (name.StartsWith(' ') || name.StartsWith('.'))
                return "The name becomes a file name, so it can't start with a space or a dot.";

            if (name.EndsWith(' ') || name.EndsWith('.'))
                return "The name becomes a file name, so it can't end in a space or a dot.";

            if (IsReserved(name))
                return $"\"{name}\" is a device name on Windows, so it can't be a file name.";

            return null;
        }

        /// <summary>
        /// Clean the name from invalid characters and check for Reseved windows names.
        /// Each refused character becomes a space and trim dots and spaces. 
        /// Fallback name used to avoid Reseved windows names.
        /// </summary>
        public static string Clean(string name, string fallback)
        {
            // Replace unsuported chars from the text.
            char[] cleanedChars = name
                .Select(x => IsInvalid(x) ? ' ' : x)
                .ToArray();

            // Conver to steing and trim dots and whitespace
            string cleaned = new string(cleanedChars).Trim('.', ' ');

            if (cleaned.Length == 0)
                return fallback;

            if (IsReserved(cleaned))
                return $"{fallback} {cleaned}";

            return cleaned;
        }

        /// <summary>
        /// A template's file name, template-k3x9q.png: five random characters from 0-9a-z. Unique
        /// only by chance - the caller draws again while its flow already has the name.
        /// </summary>
        public static string GenerateTemplateFileName()
        {
            return $"template-{RandomNumberGenerator.GetString(TemplateNameCharacters, 5)}.png";
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static bool IsInvalid(char character)
        {
            return char.IsControl(character) || InvalidCharacters.Contains(character);
        }

        // Windows reads a device name up to the first dot, so NUL.txt and NUL.tar.gz are NUL too.
        private static bool IsReserved(string name)
        {
            string stem = name.Split('.')[0].TrimEnd();

            return ReservedNames.Contains(stem);
        }
    }
}
