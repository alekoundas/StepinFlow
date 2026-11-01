using Business.Command;

namespace Business.Tests.Command
{
    /// <summary>
    /// What an area is started by, from the program an open window of it belongs to. A Steam game
    /// is started through Steam, found from the library the program sits in.
    /// </summary>
    public sealed class LaunchCommandHelperTests : IDisposable
    {
        private const string MANIFEST = "\"AppState\"\n{\n\t\"appid\"\t\t\"1013320\"\n\t\"name\"\t\t\"Firestone Idle RPG\"\n\t\"installdir\"\t\t\"Firestone\"\n\t\"UserConfig\"\n\t{\n\t\t\"appid\"\t\t\"1\"\n\t}\n}\n";

        private readonly string _folder = Path.Combine(Path.GetTempPath(), "sflw-launch-" + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, recursive: true);
        }

        [Fact]
        public void A_program_is_started_as_it_is()
        {
            LaunchCommandHelper.Of(@"C:\Tools\app.exe").ShouldBe(@"C:\Tools\app.exe");
        }

        [Fact]
        public void A_program_with_a_space_in_its_path_is_quoted()
        {
            LaunchCommandHelper.Of(@"C:\Program Files\App\app.exe").ShouldBe("\"C:\\Program Files\\App\\app.exe\"");
        }

        [Fact]
        public void A_game_in_a_steam_library_is_started_through_steam()
        {
            string program = Library(manifest: MANIFEST, "Firestone", "bin", "Firestone.exe");

            LaunchCommandHelper.Of(program).ShouldBe("steam://rungameid/1013320");
        }

        // The id belongs to whichever manifest names the folder, not to the first one there.
        [Fact]
        public void The_manifest_naming_its_folder_gives_the_id()
        {
            string program = Library(manifest: MANIFEST.Replace("Firestone\"", "Other\"", StringComparison.Ordinal), "Firestone", "Firestone.exe");
            File.WriteAllText(Path.Combine(_folder, "steamapps", "appmanifest_7.acf"), MANIFEST.Replace("1013320", "7", StringComparison.Ordinal));

            LaunchCommandHelper.Of(program).ShouldBe("steam://rungameid/7");
        }

        [Fact]
        public void A_game_no_manifest_names_is_started_by_its_program()
        {
            string program = Library(manifest: null, "Firestone", "Firestone.exe");

            LaunchCommandHelper.Of(program).ShouldBe(program.Contains(' ', StringComparison.Ordinal) ? $"\"{program}\"" : program);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // <folder>\steamapps\common\<path>, the program file at its end.
        private string Library(string? manifest, params string[] path)
        {
            string steamApps = Path.Combine(_folder, "steamapps");
            string program = Path.Combine([steamApps, "common", .. path]);

            Directory.CreateDirectory(Path.GetDirectoryName(program)!);
            File.WriteAllText(program, "");

            if (manifest != null)
                File.WriteAllText(Path.Combine(steamApps, "appmanifest_1013320.acf"), manifest);

            return program;
        }
    }
}
