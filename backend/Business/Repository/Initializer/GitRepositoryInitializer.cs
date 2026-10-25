using Core.Ports;

namespace Business.Repository.Initializer
{
    /// <summary>
    /// Makes the repository folder ready: a git repository on main when there is none, and the two
    /// files that tell git what the folder holds. The folder is the app's, so both are put back
    /// whenever they differ. Called only by <see cref="GitRepositoryService"/>, inside its lock.
    /// </summary>
    internal static class GitRepositoryInitializer
    {
        // Flows are written on Windows and executed on Linux, and the script is compared byte for byte.
        private const string ATTRIBUTES =
            "*.sflw text eol=lf\n" +
            "*.csv text eol=lf\n" +
            "*.png binary\n";

        // Everything stays out but what the app writes. A flow's CSV of values is committed: a secret is never written to it.
        private const string IGNORE =
            "*\n" +
            "!*/\n" +
            "!*.sflw\n" +
            "!*.csv\n" +
            "!*.png\n" +
            "!.gitattributes\n" +
            "!.gitignore\n";

        /// <summary>True when the repository was made just now.</summary>
        public static async Task<bool> InitializeAsync(IGitService git, string folderPath, CancellationToken ct)
        {
            bool created = git.EnsureRepositoryFolderExists(folderPath);

            await WriteIfChangedAsync(folderPath, ".gitattributes", ATTRIBUTES, ct);
            await WriteIfChangedAsync(folderPath, ".gitignore", IGNORE, ct);

            return created;
        }

        private static async Task WriteIfChangedAsync(string folderPath, string fileName, string text, CancellationToken ct)
        {
            string path = Path.Combine(folderPath, fileName);
            if (File.Exists(path) && await File.ReadAllTextAsync(path, ct) == text)
                return;

            await File.WriteAllTextAsync(path, text, ct);
        }
    }
}
