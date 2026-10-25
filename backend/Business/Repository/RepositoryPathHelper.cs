using Core.Helpers;

namespace Business.Repository
{
    /// <summary>
    /// Where a flow's files sit in the repository: <c>Name.sflw</c> at the top, and its template
    /// images in the folder <c>Name/</c> beside it - the layout the export writes.
    /// </summary>
    internal static class RepositoryPathHelper
    {
        public const string SCRIPT_EXTENSION = ".sflw";

        public static string ScriptFileOf(string flowFileName)
        {
            return flowFileName + SCRIPT_EXTENSION;
        }

        /// <summary>Its script, or anything in its template folder.</summary>
        public static bool BelongsTo(string path, string flowFileName)
        {
            string scriptFile = ScriptFileOf(flowFileName);

            return string.Equals(path, scriptFile, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(flowFileName + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The flow a path would belong to, read off the path alone: for the files of a flow that is gone.</summary>
        public static string FlowFileNameOf(string path)
        {
            int slash = path.IndexOf('/', StringComparison.Ordinal);
            if (slash >= 0)
                return path[..slash];

            if (!path.EndsWith(SCRIPT_EXTENSION, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return path[..^SCRIPT_EXTENSION.Length];
        }

        /// <summary>Ignoring case, so a picture dropped in as .PNG is still taken for one and removed when no step names it.</summary>
        public static bool IsImage(string path)
        {
            return path.EndsWith(FileNameHelper.TEMPLATE_EXTENSION, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A path the changes list could have named: inside the repository, never above it.</summary>
        public static bool IsInside(string path)
        {
            // A drive letter is rooted on Windows only, so it is refused by its colon everywhere.
            if (path.Length == 0 || Path.IsPathRooted(path) || path.Contains(':', StringComparison.Ordinal))
                return false;

            string[] segments = path.Split('/', '\\');

            return !segments.Contains("..");
        }
    }
}
