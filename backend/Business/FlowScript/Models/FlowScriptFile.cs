namespace Business.FlowScript.Models
{
    /// <summary>
    /// A .sflw file as it sits on disk: its text, and the template images in the folder named after
    /// it, beside it. Read the same way for an import and for a look before one.
    /// </summary>
    public sealed class FlowScriptFile
    {
        public string Script { get; private init; } = string.Empty;

        public string TemplateFolderPath { get; private init; } = string.Empty;

        /// <summary>By file name, ignoring case, as Windows and macOS find a file.</summary>
        public Dictionary<string, byte[]> Templates { get; private init; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public static async Task<FlowScriptFile> ReadAsync(string scriptPath, CancellationToken ct)
        {
            string script = await File.ReadAllTextAsync(scriptPath, ct);

            string directory = Path.GetDirectoryName(scriptPath) ?? string.Empty;
            string templateFolderPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(scriptPath));

            Dictionary<string, byte[]> templates = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(templateFolderPath))
            {
                foreach (string path in Directory.EnumerateFiles(templateFolderPath))
                    templates[Path.GetFileName(path)] = await File.ReadAllBytesAsync(path, ct);
            }

            return new FlowScriptFile
            {
                Script = script,
                TemplateFolderPath = templateFolderPath,
                Templates = templates,
            };
        }
    }
}
