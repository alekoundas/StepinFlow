using Business.FlowScript;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A file looked at before it is imported: what it would replace, what it would clash with, and
    /// what is missing beside it, with nothing saved. Files are written to a folder of their own.
    /// </summary>
    public sealed class FlowScriptPreviewServiceTests : IDisposable
    {
        private const string LOGIN_ID = "3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d";
        private const string OTHER_ID = "9a1e2f3b-4c5d-4e6f-8a9b-0c1d2e3f4a5b";

        // As much of a PNG as the importer looks at: its signature.
        private static readonly byte[] PNG = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "sflw-preview-" + Guid.NewGuid().ToString("N")[..8]);
        private readonly FlowScriptExporter _exporter;
        private readonly FlowScriptImporter _importer;
        private readonly FlowScriptPreviewService _preview;

        public FlowScriptPreviewServiceTests()
        {
            _exporter = new FlowScriptExporter(_database, new Printer());
            _importer = new FlowScriptImporter(new Scanner(), TestDataService.For(_database));
            _preview = new FlowScriptPreviewService(new Scanner(), _exporter, TestDataService.For(_database));

            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_folder))
                Directory.Delete(_folder, recursive: true);
        }

        private static string Script(string name, string id, string stepName)
        {
            return
                $"Flow:    {name}\n" +
                $"Id:      {id}\n\n" +
                "Inputs:\n" +
                "  <[ email ]>       default <[ ops@example.com ]>\n" +
                "  <[ password ]>    secret\n\n" +
                "Templates:\n" +
                "  <[ login-button.png ]>    click 20 6\n" +
                "  <[ logo.png ]>            click 2 2\n\n" +
                "Steps:\n\n" +
                $"Find Image  <[ {stepName} ]>   template <[ login-button.png ]>  template <[ logo.png ]>\n" +
                " Success:\n" +
                "  Click\n";
        }

        // Only login-button.png is beside it, in the folder named after the file.
        private async Task<string> WriteAsync(string fileName, string script)
        {
            string templateFolder = Path.Combine(_folder, fileName);
            Directory.CreateDirectory(templateFolder);
            await File.WriteAllBytesAsync(Path.Combine(templateFolder, "login-button.png"), PNG, Ct);

            string scriptPath = Path.Combine(_folder, fileName + ".sflw");
            await File.WriteAllTextAsync(scriptPath, script, Ct);

            return scriptPath;
        }

        private int FlowCount()
        {
            using AppDbContext db = _database.CreateDbContext();
            return db.Flows.Count();
        }

        [Fact]
        public async Task A_file_with_an_Id_no_flow_has_is_a_new_flow_and_nothing_is_saved()
        {
            string path = await WriteAsync("Login", Script("Login", LOGIN_ID, "Find login"));

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            preview.Errors.ShouldBeEmpty();
            (preview.FlowName, preview.PublicId).ShouldBe(("Login", Guid.Parse(LOGIN_ID)));
            (preview.Target.FlowId, preview.Target.NameTaken).ShouldBe((null, null));
            preview.ReplacedScript.ShouldBeEmpty();
            preview.StepCount.ShouldBe(2);
            preview.Inputs.Select(x => (x.Name, x.DefaultValue, x.IsSecret)).ShouldBe([("email", "ops@example.com", false), ("password", "", true)]);
            FlowCount().ShouldBe(0);
        }

        [Fact]
        public async Task A_template_not_beside_the_file_is_missing()
        {
            string path = await WriteAsync("Login", Script("Login", LOGIN_ID, "Find login"));

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            preview.TemplateNames.ShouldBe(["login-button.png", "logo.png"]);
            preview.MissingTemplates.ShouldBe(["logo.png"]);
            preview.TemplateFolderPath.ShouldBe(Path.Combine(_folder, "Login"));
        }

        [Fact]
        public async Task A_file_named_png_that_is_not_one_is_missing()
        {
            string path = await WriteAsync("Login", Script("Login", LOGIN_ID, "Find login"));
            await File.WriteAllBytesAsync(Path.Combine(_folder, "Login", "logo.png"), [0xFF, 0xD8, 0xFF, 0xE0], Ct);

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            preview.MissingTemplates.ShouldBe(["logo.png"]);
        }

        // The import's write path finds the flow by its Id, so the preview does too.
        [Fact]
        public async Task A_file_with_a_flow_s_Id_replaces_it_and_shows_it_as_it_is()
        {
            FlowScriptImportResultDto imported = await _importer.ImportTextAsync(Script("Login", LOGIN_ID, "Find login"), new Dictionary<string, byte[]>(), Ct);
            string path = await WriteAsync("Login", Script("Login", LOGIN_ID, "Find the sign-in button"));

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            (preview.Target.FlowId, preview.Target.FlowName, preview.Target.NameTaken).ShouldBe((imported.FlowId, "Login", null));
            preview.ReplacedScript.ShouldContain("Find login");
            preview.Script.ShouldContain("Find the sign-in button");
            FlowCount().ShouldBe(1);
        }

        [Fact]
        public async Task A_name_another_flow_has_is_refused_whatever_its_case()
        {
            await _importer.ImportTextAsync(Script("Login", LOGIN_ID, "Find login"), new Dictionary<string, byte[]>(), Ct);
            string path = await WriteAsync("login", Script("login", OTHER_ID, "Find login"));

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            preview.Target.FlowId.ShouldBeNull();
            preview.Target.NameTaken.ShouldNotBeNull().ShouldContain("Another flow is already called \"Login\"");
        }

        [Fact]
        public async Task A_file_that_does_not_read_cleanly_says_where()
        {
            string script = Script("Login", LOGIN_ID, "Find login").Replace("Find Image", "Fnid Image", StringComparison.Ordinal);
            string path = await WriteAsync("Login", script);

            FlowScriptPreviewDto preview = await _preview.PreviewAsync(path, Ct);

            // The branch under it is left without a step, and says so too.
            FlowScriptErrorDto error = preview.Errors[0];
            (error.Line, error.Message).ShouldBe((14, @"Unexpected ""Fnid""."));
            preview.Script.ShouldBe(script);
        }

        [Fact]
        public async Task A_file_that_is_not_there_says_so()
        {
            FlowScriptPreviewDto preview = await _preview.PreviewAsync(Path.Combine(_folder, "Gone.sflw"), Ct);

            preview.Errors.ShouldHaveSingleItem().Code.ShouldBe(nameof(DiagnosticCodeEnum.FILE_MISSING));
        }

        // ================================================================
        // The export folder
        // ================================================================

        [Fact]
        public async Task The_scripts_in_a_folder_are_listed_newest_first_with_their_templates()
        {
            string older = await WriteAsync("Older", Script("Older", LOGIN_ID, "Find login"));
            string newer = await WriteAsync("Newer", Script("Newer", OTHER_ID, "Find login"));
            File.SetLastWriteTime(older, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Local));
            File.SetLastWriteTime(newer, new DateTime(2026, 10, 17, 9, 0, 0, DateTimeKind.Local));
            await File.WriteAllTextAsync(Path.Combine(_folder, "notes.txt"), "not a script", Ct);

            List<FlowScriptFileDto> scripts = _exporter.ListScripts(_folder);

            scripts.Select(x => (x.FileName, x.TemplateCount)).ShouldBe([("Newer.sflw", 1), ("Older.sflw", 1)]);
            scripts[0].Path.ShouldBe(newer);
        }

        [Fact]
        public void A_folder_that_is_not_there_lists_nothing()
        {
            _exporter.ListScripts(Path.Combine(_folder, "missing")).ShouldBeEmpty();
        }
    }
}
