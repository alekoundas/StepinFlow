namespace Business.Shell
{
    /// <summary>
    /// What starts an app, worked out from the program an open window of it belongs to: an area's
    /// Started by, filled in when the app is picked.
    ///
    /// A Steam game is started through Steam. Its program, started on its own, restarts itself
    /// through Steam or refuses to start at all. A Steam library is laid out the same way on
    /// Windows, macOS and Linux, so finding one is not tied to Windows.
    /// </summary>
    public static class LaunchCommandHelper
    {
        private const string STEAM_LINK = "steam://rungameid/";

        /// <summary>The program, quoted when its path has a space, or its game's Steam link.</summary>
        public static string Of(string programPath)
        {
            string? steamLink = SteamLinkOf(programPath);
            if (steamLink != null)
                return steamLink;

            if (programPath.Contains(' ', StringComparison.Ordinal))
                return $"\"{programPath}\"";

            return programPath;
        }


        // ================================================================
        // Private methods
        // ================================================================

        // ...\steamapps\common\<install folder>\...\game.exe, with a steamapps\appmanifest_<id>.acf
        // that names the install folder.
        private static string? SteamLinkOf(string programPath)
        {
            DirectoryInfo? folder = new FileInfo(programPath).Directory;

            while (folder?.Parent?.Parent != null)
            {
                DirectoryInfo common = folder.Parent;
                DirectoryInfo steamApps = folder.Parent.Parent;

                if (common.Name.Equals("common", StringComparison.OrdinalIgnoreCase) &&
                    steamApps.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                {
                    string? gameId = GameIdOf(steamApps, folder.Name);
                    if (gameId == null)
                        return null;

                    return STEAM_LINK + gameId;
                }

                folder = folder.Parent;
            }

            return null;
        }

        private static string? GameIdOf(DirectoryInfo steamApps, string installFolder)
        {
            try
            {
                foreach (FileInfo manifest in steamApps.EnumerateFiles("appmanifest_*.acf"))
                {
                    Dictionary<string, string> values = ValuesOf(File.ReadLines(manifest.FullName));

                    if (values.TryGetValue("installdir", out string? folder) &&
                        folder.Equals(installFolder, StringComparison.OrdinalIgnoreCase) &&
                        values.TryGetValue("appid", out string? gameId))
                        return gameId;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // A library on a drive that went away, or a manifest Steam is writing. The program
                // itself is then the next best thing.
            }

            return null;
        }

        // A manifest's top level pairs: "appid"		"1013320". Its nested blocks have pairs of their own,
        // and the first of each name is the one wanted.
        private static Dictionary<string, string> ValuesOf(IEnumerable<string> lines)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string line in lines)
            {
                string[] parts = line.Split('"');
                if (parts.Length < 5)
                    continue;

                values.TryAdd(parts[1], parts[3]);
            }

            return values;
        }
    }
}
