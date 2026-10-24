using Core.Helpers;

namespace Business.FlowScript.Models
{
    /// <summary>
    /// A .sflw file as it sits on disk: its text, and the template images in the folder named after
    /// it, beside it. Read the same way for an import and for a look before one.
    /// </summary>
    public sealed class FlowScriptFile
    {
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public string Script { get; private init; } = string.Empty;

        public string TemplateFolderPath { get; private init; } = string.Empty;

        /// <summary>By file name, ignoring case, as Windows and macOS find a file.</summary>
        public Dictionary<string, byte[]> Templates { get; private init; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public static async Task<FlowScriptFile> ReadAsync(string scriptPath, CancellationToken ct)
        {
            string script = await File.ReadAllTextAsync(scriptPath, ct);

            string directory = Path.GetDirectoryName(scriptPath) ?? string.Empty;
            string templateFolderPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(scriptPath));

            // PNGs only, by their bytes as well as their name: anything else is not there, so its template comes without a picture.
            Dictionary<string, byte[]> templates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(templateFolderPath))
            {
                foreach (string path in Directory.EnumerateFiles(templateFolderPath).Where(IsPngName))
                {
                    byte[] bytes = await File.ReadAllBytesAsync(path, ct);
                    if (bytes.AsSpan().StartsWith(PngSignature))
                        templates[Path.GetFileName(path)] = bytes;
                }
            }

            return new FlowScriptFile
            {
                Script = script,
                TemplateFolderPath = templateFolderPath,
                Templates = templates,
            };
        }

        // Ignoring case, as Windows and macOS find a file; the script's own name is held to lowercase.
        internal static bool IsPngName(string path)
        {
            return path.EndsWith(FileNameHelper.TEMPLATE_EXTENSION, StringComparison.OrdinalIgnoreCase);
        }
    }
}
