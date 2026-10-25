using System.Buffers.Binary;

using Business.FlowScript;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A flow in a real database, exported to files, imported back over itself and exported again.
    /// Identical bytes are not enough on their own: they hid an importer that dropped every field
    /// the printer never printed. So the rows are compared too.
    /// </summary>
    public sealed class ScriptImportExportTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "sflw-" + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_folder))
                Directory.Delete(_folder, recursive: true);
        }

        private FlowScriptExporter Exporter()
        {
            return new FlowScriptExporter(_database, new Printer());
        }

        private FlowScriptImporter Importer()
        {
            return new FlowScriptImporter(new Scanner(), TestDataService.For(_database));
        }

        private sealed record RoundTrip(int FlowId, FlowScriptImportResultDto Imported, string First, string Second, ScriptRows Before, ScriptRows After);

        private async Task<RoundTrip> ExportImportExportAsync()
        {
            int flowId = Seed();

            FlowScriptExportResultDto exported = await Exporter().ExportAsync(flowId, _folder, Ct);
            string first = await File.ReadAllTextAsync(exported.ScriptPath, Ct);
            ScriptRows before = await ScriptRows.LoadAsync(_database, flowId);

            FlowScriptImportResultDto imported = await Importer().ImportAsync(exported.ScriptPath, Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));

            ScriptRows after = await ScriptRows.LoadAsync(_database, imported.FlowId);
            FlowScriptExportResultDto again = await Exporter().ExportAsync(imported.FlowId, _folder, Ct);
            string second = await File.ReadAllTextAsync(again.ScriptPath, Ct);

            return new RoundTrip(flowId, imported, first, second, before, after);
        }

        // ================================================================
        // The round trip
        // ================================================================

        [Fact]
        public async Task Export_import_export_gives_back_the_same_bytes()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.Second.ShouldBe(trip.First);
        }

        [Fact]
        public async Task The_import_replaces_the_flow_rather_than_adding_one()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.Imported.FlowId.ShouldBe(trip.FlowId);
            trip.Imported.MissingTemplates.ShouldBeEmpty();
        }

        [Fact]
        public async Task Every_area_comes_back_field_for_field()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Areas.Count.ShouldBe(trip.Before.Areas.Count);
            foreach (FlowArea area in trip.Before.Areas)
            {
                FlowArea? match = trip.After.Areas.FirstOrDefault(x => x.Name == area.Name);
                List<string> differences = ScriptRows.Differences(area, match);

                if (trip.Before.AreaName(area.ParentFlowAreaId) != trip.After.AreaName(match?.ParentFlowAreaId))
                    differences.Add("parent");

                differences.ShouldBeEmpty(area.Name);
            }
        }

        [Fact]
        public async Task Every_point_comes_back_field_for_field()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Points.Count.ShouldBe(trip.Before.Points.Count);
            foreach (FlowPoint point in trip.Before.Points)
            {
                FlowPoint? match = trip.After.Points.FirstOrDefault(x => x.Name == point.Name);
                List<string> differences = ScriptRows.Differences(point, match);

                if (trip.Before.AreaName(point.FlowAreaId) != trip.After.AreaName(match?.FlowAreaId))
                    differences.Add("area");

                differences.ShouldBeEmpty(point.Name);
            }
        }

        // The name too: it is the file the template went out as, and comes back as it was.
        [Fact]
        public async Task Every_template_comes_back_field_for_field_with_its_picture()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Templates.Count.ShouldBe(trip.Before.Templates.Count);
            foreach (FlowStepTemplate template in trip.Before.Templates)
            {
                FlowStepTemplate? match = trip.After.Templates.FirstOrDefault(x =>
                    trip.After.StepName(x.FlowStepId) == trip.Before.StepName(template.FlowStepId) && x.OrderNumber == template.OrderNumber);

                ScriptRows.Differences(template, match).ShouldBeEmpty(template.FlowTemplate.Name);
                ScriptRows.Differences(template.FlowTemplate, match?.FlowTemplate).ShouldBeEmpty(template.FlowTemplate.Name);
            }
        }

        // The description's blank line too: it is a bare # in the file.
        [Fact]
        public async Task The_flow_comes_back_field_for_field()
        {
            RoundTrip trip = await ExportImportExportAsync();

            ScriptRows.Differences(trip.Before.Flow, trip.After.Flow).ShouldBeEmpty();
            trip.After.Flow.Description.ShouldContain("\n\n");
        }

        [Fact]
        public async Task Every_input_comes_back_with_its_default()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Inputs.Count.ShouldBe(trip.Before.Inputs.Count);
            foreach (FlowCsvColumn input in trip.Before.Inputs)
            {
                FlowCsvColumn? match = trip.After.Inputs.FirstOrDefault(x => x.Name == input.Name);
                ScriptRows.Differences(input, match).ShouldBeEmpty(input.Name);
            }
        }

        [Fact]
        public async Task A_steps_match_mode_comes_back()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS);
        }

        // An export over an earlier one brings it up to date, and leaves alone what is not a PNG.
        [Fact]
        public async Task Exporting_again_removes_the_images_the_flow_no_longer_uses()
        {
            int flowId = Seed();
            FlowScriptExportResultDto first = await Exporter().ExportAsync(flowId, _folder, Ct);
            await File.WriteAllBytesAsync(Path.Combine(first.TemplateFolderPath, "template-gone1.png"), Png(4, 4), Ct);
            await File.WriteAllTextAsync(Path.Combine(first.TemplateFolderPath, "README.md"), "notes", Ct);

            FlowScriptExportResultDto again = await Exporter().ExportAsync(flowId, _folder, Ct);

            File.Exists(Path.Combine(again.TemplateFolderPath, "template-gone1.png")).ShouldBeFalse();
            File.Exists(Path.Combine(again.TemplateFolderPath, "README.md")).ShouldBeTrue();
            Directory.GetFiles(again.TemplateFolderPath, "*.png").Length.ShouldBe(again.TemplateCount);
        }

        // ================================================================
        // Importing
        // ================================================================

        // Parse and bind before the transaction, so half a flow is never written.
        [Fact]
        public async Task A_script_with_a_typo_is_refused_and_leaves_the_flow_untouched()
        {
            int flowId = Seed();
            FlowScriptExportResultDto exported = await Exporter().ExportAsync(flowId, _folder, Ct);
            string script = await File.ReadAllTextAsync(exported.ScriptPath, Ct);

            FlowScriptImportResultDto refused = await Importer().ImportTextAsync(script.Replace("Find Image", "Fnid Image", StringComparison.Ordinal), new Dictionary<string, byte[]>(), Ct);

            refused.IsSuccess.ShouldBeFalse();
            refused.Errors[0].Message.ShouldBe(@"Unexpected ""Fnid"".");
            FlowScriptExportResultDto after = await Exporter().ExportAsync(flowId, _folder, Ct);
            (await File.ReadAllTextAsync(after.ScriptPath, Ct)).ShouldBe(script);
        }

        // A warning does not stop the import; it comes back beside the result for the form to show.
        [Fact]
        public async Task A_warning_is_reported_and_the_flow_is_still_imported()
        {
            string script =
                "Flow:    Warned\n" +
                "Id:      3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6e\n\n" +
                "Steps:\n" +
                "Wait 800ms\n" +
                "# nothing below\n";

            FlowScriptImportResultDto imported = await Importer().ImportTextAsync(script, new Dictionary<string, byte[]>(), Ct);

            imported.IsSuccess.ShouldBeTrue();
            imported.Errors.ShouldBeEmpty();
            FlowScriptErrorDto warning = imported.Warnings.ShouldHaveSingleItem();
            (warning.Code, warning.Line).ShouldBe((nameof(DiagnosticCodeEnum.COMMENT_UNATTACHED), 6));
        }

        // Only a template made in the app is given a name. One written by hand keeps its own in the
        // database and in the next export.
        [Fact]
        public async Task A_hand_written_template_name_is_kept_through_import_and_export()
        {
            byte[] button = Png(40, 12);
            byte[] logo = Png(8, 8);

            string templateFolder = Path.Combine(_folder, "Hand written");
            Directory.CreateDirectory(templateFolder);
            await File.WriteAllBytesAsync(Path.Combine(templateFolder, "login-button.png"), button, Ct);
            await File.WriteAllBytesAsync(Path.Combine(templateFolder, "Company logo.png"), logo, Ct);

            string scriptPath = Path.Combine(_folder, "Hand written.sflw");
            await File.WriteAllTextAsync(scriptPath,
                "Flow:    Hand written\n" +
                "Id:      3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d\n\n" +
                "Templates:\n" +
                "  <[ login-button.png ]>    click 20 6\n" +
                "  <[ Company logo.png ]>    click 2 2\n\n" +
                "Steps:\n\n" +
                "Find Image  <[ Find login ]>   template <[ login-button.png ]>  template <[ Company logo.png ]>\n", Ct);

            FlowScriptImportResultDto imported = await Importer().ImportAsync(scriptPath, Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));
            imported.MissingTemplates.ShouldBeEmpty();

            ScriptRows rows = await ScriptRows.LoadAsync(_database, imported.FlowId);
            rows.Templates.Select(x => x.FlowTemplate.Name).ShouldBe(["login-button.png", "Company logo.png"], ignoreOrder: true);

            FlowScriptExportResultDto exported = await Exporter().ExportAsync(imported.FlowId, Path.Combine(_folder, "out"), Ct);
            string script = await File.ReadAllTextAsync(exported.ScriptPath, Ct);

            script.ShouldContain("<[ login-button.png ]>");
            script.ShouldContain("<[ Company logo.png ]>");
            (await File.ReadAllBytesAsync(Path.Combine(exported.TemplateFolderPath, "login-button.png"), Ct)).ShouldBe(button);
            (await File.ReadAllBytesAsync(Path.Combine(exported.TemplateFolderPath, "Company logo.png"), Ct)).ShouldBe(logo);
        }

        // Only the folder's PNGs are read, by their bytes: a JPEG given a .png name is not one.
        [Fact]
        public async Task A_file_named_png_that_is_not_one_imports_its_template_without_a_picture()
        {
            string templateFolder = Path.Combine(_folder, "Renamed");
            Directory.CreateDirectory(templateFolder);
            await File.WriteAllBytesAsync(Path.Combine(templateFolder, "logo.png"), [0xFF, 0xD8, 0xFF, 0xE0], Ct);

            string scriptPath = Path.Combine(_folder, "Renamed.sflw");
            await File.WriteAllTextAsync(scriptPath,
                "Flow:    Renamed\n" +
                "Id:      3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d\n\n" +
                "Templates:\n" +
                "  <[ logo.png ]>    click 2 2\n\n" +
                "Steps:\n\n" +
                "Find Image  <[ Find logo ]>   template <[ logo.png ]>\n", Ct);

            FlowScriptImportResultDto imported = await Importer().ImportAsync(scriptPath, Ct);

            imported.IsSuccess.ShouldBeTrue();
            imported.MissingTemplates.ShouldBe(["logo.png"]);
        }

        // The header describes a picture once, so every step that names the file links to one row,
        // and the export writes the file once.
        [Fact]
        public async Task Steps_naming_one_file_share_its_picture()
        {
            byte[] button = Png(40, 12);

            string templateFolder = Path.Combine(_folder, "Shared");
            Directory.CreateDirectory(templateFolder);
            await File.WriteAllBytesAsync(Path.Combine(templateFolder, "template-k3x9q.png"), button, Ct);

            string scriptPath = Path.Combine(_folder, "Shared.sflw");
            await File.WriteAllTextAsync(scriptPath,
                "Flow:    Shared\n" +
                "Id:      7d2e4f61-0a1b-4c2d-8e3f-4a5b6c7d8e9f\n\n" +
                "Templates:\n" +
                "  <[ template-k3x9q.png ]>    click 20 6\n\n" +
                "Steps:\n\n" +
                "Find Image  <[ Find login ]>   template <[ template-k3x9q.png ]> accuracy 0.9\n" +
                "Find Image  <[ Find login again ]>   template <[ template-k3x9q.png ]> accuracy 0.7 required\n", Ct);

            FlowScriptImportResultDto imported = await Importer().ImportAsync(scriptPath, Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));

            ScriptRows rows = await ScriptRows.LoadAsync(_database, imported.FlowId);
            rows.Templates.Count.ShouldBe(2);
            rows.Templates.Select(x => x.FlowTemplateId).Distinct().ShouldHaveSingleItem();
            rows.Templates.Select(x => (x.Accuracy, x.IsRequired)).ShouldBe([(0.9f, false), (0.7f, true)], ignoreOrder: true);

            FlowScriptExportResultDto exported = await Exporter().ExportAsync(imported.FlowId, Path.Combine(_folder, "out"), Ct);
            string script = await File.ReadAllTextAsync(exported.ScriptPath, Ct);

            script.Split('\n').Count(x => x.TrimStart().StartsWith("<[ template-k3x9q.png ]>", StringComparison.Ordinal)).ShouldBe(1);
            Directory.GetFiles(exported.TemplateFolderPath).ShouldHaveSingleItem();
        }

        // ================================================================
        // The flow
        // ================================================================

        private int Seed()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow
            {
                Name = "Login and add to cart",
                PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111"),
                Description = "Logs in to Swag Labs and adds the backpack to the cart.\n\nThe cart badge shows the count.",
            };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowArea browser = new FlowArea { Name = "Browser", FlowId = flow.Id, IsMain = true, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 };
            FlowArea screen = new FlowArea { Name = "Screen", FlowId = flow.Id, Type = FlowAreaTypeEnum.MONITOR, ScalesWith = ScalesWithEnum.DPI };
            FlowArea box = new FlowArea { Name = "Box", FlowId = flow.Id, Type = FlowAreaTypeEnum.CUSTOM, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 10, LocationY = 20, Width = 300, Height = 200, AuthoredDpi = 96 };
            FlowArea inventoryTab = new FlowArea { Name = "Inventory tab", FlowId = flow.Id, Type = FlowAreaTypeEnum.BROWSER_TAB, ProcessName = "chrome.exe", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH, TabMatchOn = TabMatchOnEnum.URL, TabMatchValue = "/inventory.html" };
            FlowArea gameClient = new FlowArea { Name = "Game client", FlowId = flow.Id, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "game.exe", UseClientArea = false, ScalesWith = ScalesWithEnum.AREA };
            db.FlowAreas.AddRange(browser, screen, box, inventoryTab, gameClient);
            db.SaveChanges();

            FlowArea badge = new FlowArea { Name = "Cart badge", FlowId = flow.Id, Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browser.Id, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.88f, RatioY = 0f, RatioWidth = 0.12f, RatioHeight = 0.10f };
            FlowArea header = new FlowArea { Name = "Header", FlowId = flow.Id, Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browser.Id, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 0, LocationY = 0, Width = 1920, Height = 90, AuthoredDpi = 120 };
            FlowArea game = new FlowArea { Name = "Game", FlowId = flow.Id, Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = browser.Id, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.1f, RatioY = 0.2f, RatioWidth = 0.8f, RatioHeight = 0.7f, ScalesWith = ScalesWithEnum.AREA };
            db.FlowAreas.AddRange(badge, header, game);

            FlowPoint hamburger = new FlowPoint { Name = "Hamburger", FlowId = flow.Id, FlowAreaId = browser.Id, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.95f, RatioY = 0.05f };
            FlowPoint origin = new FlowPoint { Name = "Origin", FlowId = flow.Id, OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 12, LocationY = -12 };
            FlowPoint menu = new FlowPoint { Name = "Menu", FlowId = flow.Id, FlowAreaId = browser.Id, OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 40, LocationY = 8, AuthoredDpi = 120 };
            db.FlowPoints.AddRange(hamburger, origin, menu);

            db.FlowViewports.AddRange(
                new FlowViewport { FlowId = flow.Id, Width = 1920, Height = 1080, OrderNumber = 0 },
                new FlowViewport { FlowId = flow.Id, Width = 390, Height = 844, OrderNumber = 1 });

            db.FlowCsvColumns.AddRange(
                new FlowCsvColumn { FlowId = flow.Id, Name = "username", OrderNumber = 0, DefaultValue = "standard_user" },
                new FlowCsvColumn { FlowId = flow.Id, Name = "password", OrderNumber = 1, IsSecret = true });

            db.SaveChanges();

            FlowStep Add(FlowStep step, int? parent)
            {
                step.RootId = flow.Id;
                step.OrderNumber = db.FlowSteps.Count();
                step.ParentFlowStepId = parent;
                step.FlowId = parent == null ? flow.Id : null;
                db.FlowSteps.Add(step);
                db.SaveChanges();
                return step;
            }

            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = "Sign in" }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe https://www.saucedemo.com", FlowAreaId = browser.Id, CodeComment = "A fresh profile every time." }, null);

            // Two variants and only one required, so an importer forcing every template required -
            // which this one did - shows up as a changed row.
            FlowStep find = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = browser.Id, TemplateMatchMode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS }, null);
            FlowTemplate username = new FlowTemplate { FlowId = flow.Id, Name = "template-u9d3n.png", TemplateImage = Png(120, 24), ClickOffsetX = 60, ClickOffsetY = 12, AuthoredFlowAreaWidth = 1920, AuthoredFlowAreaHeight = 1080, AuthoredDpi = 120 };
            FlowTemplate usernameFocused = new FlowTemplate { FlowId = flow.Id, Name = "template-w2h6r.png", TemplateImage = Png(80, 20), ClickOffsetX = -4, ClickOffsetY = 30, AuthoredDpi = 96 };
            db.FlowStepTemplates.AddRange(
                new FlowStepTemplate { FlowStepId = find.Id, FlowTemplate = username, OrderNumber = 0, IsRequired = false, Accuracy = 0.96f },
                new FlowStepTemplate { FlowStepId = find.Id, FlowTemplate = usernameFocused, OrderNumber = 1, IsRequired = true, Accuracy = 0.9f });
            db.SaveChanges();

            FlowStep found = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS }, find.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReferenceId = find.Id }, found.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, found.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, found.Id);

            FlowStep missed = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE }, find.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPointId = hamburger.Id }, missed.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, missed.Id);

            FlowStep total = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = @"total: (\d+)", FlowAreaId = badge.Id, ResultExtractPattern = @"(\d+)", TimeoutMilliseconds = 10000 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = "Order is large", FlowStepReferenceId = total.Id, ConditionType = ConditionTypeEnum.GREATER_THAN, ConditionText = "100" }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Banner cleared", SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionType = ConditionTypeEnum.IS_NOT_EMPTY, FlowAreaId = header.Id }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = hamburger.Id, FlowPointEndId = origin.Id, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = CursorScrollDirectionTypeEnum.DOWN, LoopCount = 3 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800, WaitForMillisecondsMax = 1200 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome.exe", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH, WindowWidth = 1280, WindowHeight = 720 }, null);

            FlowStep loop = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.LOOP, Name = "Retry", LoopCount = 5 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReferenceId = find.Id }, loop.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK }, loop.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.BREAK, FlowStepReferenceId = loop.Id }, loop.Id);

            FlowStep end = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = false, Message = "did not reach the products page" }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.KILL_PROCESS, RunCommandValue = "chrome.exe" }, end.Id);

            return flow.Id;
        }

        // Only as much of a png as the importer reads: the signature, then an IHDR chunk with the size.
        private static byte[] Png(int width, int height)
        {
            byte[] png = new byte[33];
            byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            signature.CopyTo(png, 0);
            png[11] = 13;
            "IHDR"u8.CopyTo(png.AsSpan(12));
            BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
            BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);
            return png;
        }
    }
}
