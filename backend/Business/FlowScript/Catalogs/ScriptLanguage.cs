namespace Business.FlowScript.Catalogs
{
    /// <summary>
    /// The version of the script language this app writes, which is also the newest it reads. Raised
    /// only when the grammar changes, never by an app release, and listed in flow-format.md with the
    /// release that brought it.
    /// </summary>
    internal static class ScriptLanguage
    {
        public const int CURRENT_VERSION = 1;

        // Script Language: v1
        public const string VERSION_PREFIX = "v";
    }
}
