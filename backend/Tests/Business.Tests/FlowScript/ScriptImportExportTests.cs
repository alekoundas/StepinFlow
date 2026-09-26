using System.Buffers.Binary;

using Business.FlowScript;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

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
            return new FlowScriptImporter(_database, new Parser());
        }

        private sealed record RoundTrip(int FlowId, FlowImportResultDto Imported, string First, string Second, ScriptRows Before, ScriptRows After);

        private async Task<RoundTrip> ExportImportExportAsync()
        {
            int flowId = Seed();

            FlowExportResultDto exported = await Exporter().ExportAsync(flowId, _folder, Ct);
            string first = await File.ReadAllTextAsync(exported.ScriptPath, Ct);
            ScriptRows before = await ScriptRows.LoadAsync(_database, flowId);

            FlowImportResultDto imported = await Importer().ImportAsync(exported.ScriptPath, Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));

            ScriptRows after = await ScriptRows.LoadAsync(_database, imported.FlowId);
            FlowExportResultDto again = await Exporter().ExportAsync(imported.FlowId, _folder, Ct);
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

        // Named after the file it went out as, so the name is the one field allowed to change.
        [Fact]
        public async Task Every_template_comes_back_field_for_field_with_its_picture()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Templates.Count.ShouldBe(trip.Before.Templates.Count);
            foreach (FlowStepTemplate template in trip.Before.Templates)
            {
                FlowStepTemplate? match = trip.After.Templates.FirstOrDefault(x =>
                    trip.After.StepName(x.FlowStepId) == trip.Before.StepName(template.FlowStepId) && x.OrderNumber == template.OrderNumber);

                ScriptRows.Differences(template, match, "Name").ShouldBeEmpty(template.Name);
            }
        }

        [Fact]
        public async Task A_steps_match_mode_comes_back()
        {
            RoundTrip trip = await ExportImportExportAsync();

            trip.After.Steps.Single(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE).TemplateMatchMode.ShouldBe(TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS);
        }

        // ================================================================
        // Importing
        // ================================================================

        [Fact]
        public async Task A_template_with_no_header_line_is_clicked_in_its_middle()
        {
            Directory.CreateDirectory(_folder);
            await File.WriteAllBytesAsync(Path.Combine(_folder, "button.png"), Png(41, 20), Ct);
            string script = """
                Flow:    Hand written
                Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b112

                Steps:
                Find Image  "Find the button"   template "button.png"
                """;

            FlowImportResultDto imported = await Importer().ImportTextAsync(script, _folder, Ct);

            imported.IsSuccess.ShouldBeTrue();
            FlowStepTemplate button = (await ScriptRows.LoadAsync(_database, imported.FlowId)).Templates.Single();
            (button.ClickOffsetX, button.ClickOffsetY).ShouldBe((21, 10));
            button.IsRequired.ShouldBeFalse();
            button.Accuracy.ShouldBe(ScriptTemplate.DefaultAccuracy(TemplateMatchModeEnum.SHAPE));
        }

        // Parse and bind before the transaction, so half a flow is never written.
        [Fact]
        public async Task A_script_with_a_typo_is_refused_and_leaves_the_flow_untouched()
        {
            int flowId = Seed();
            FlowExportResultDto exported = await Exporter().ExportAsync(flowId, _folder, Ct);
            string script = await File.ReadAllTextAsync(exported.ScriptPath, Ct);

            FlowImportResultDto refused = await Importer().ImportTextAsync(script.Replace("Find Image", "Fnid Image", StringComparison.Ordinal), _folder, Ct);

            refused.IsSuccess.ShouldBeFalse();
            refused.Errors[0].Message.ShouldStartWith(@"""Fnid"" is not a step");
            FlowExportResultDto after = await Exporter().ExportAsync(flowId, _folder, Ct);
            (await File.ReadAllTextAsync(after.ScriptPath, Ct)).ShouldBe(script);
        }

        // ================================================================
        // The flow
        // ================================================================

        private int Seed()
        {
            using AppDbContext db = _database.CreateDbContext();

            Flow flow = new Flow { Name = "Login and add to cart", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111") };
            db.Flows.Add(flow);
            db.SaveChanges();

            FlowArea browser = new FlowArea { Name = "Browser", FlowId = flow.Id, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 };
            FlowArea screen = new FlowArea { Name = "Screen", FlowId = flow.Id, Type = FlowAreaTypeEnum.MONITOR, ScalesWith = ScalesWithEnum.DPI };
            FlowArea box = new FlowArea { Name = "Box", FlowId = flow.Id, Type = FlowAreaTypeEnum.CUSTOM, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 10, LocationY = 20, Width = 300, Height = 200, AuthoredDpi = 96 };
            db.FlowAreas.AddRange(browser, screen, box);
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
                new FlowCsvColumn { FlowId = flow.Id, Name = "username", OrderNumber = 0 },
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

            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.MARKER, Name = "Sign in" }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe https://www.saucedemo.com", CodeComment = "A fresh profile every time." }, null);

            // Two variants and only one required, so an importer forcing every template required -
            // which this one did - shows up as a changed row.
            FlowStep find = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = browser.Id, TemplateMatchMode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS }, null);
            db.FlowStepTemplates.AddRange(
                new FlowStepTemplate { FlowStepId = find.Id, Name = "username field", OrderNumber = 0, TemplateImage = Png(120, 24), IsRequired = false, Accuracy = 0.96f, ClickOffsetX = 60, ClickOffsetY = 12, AuthoredFlowAreaWidth = 1920, AuthoredFlowAreaHeight = 1080, AuthoredDpi = 120 },
                new FlowStepTemplate { FlowStepId = find.Id, Name = "username alt", OrderNumber = 1, TemplateImage = Png(80, 20), IsRequired = true, Accuracy = 0.9f, ClickOffsetX = -4, ClickOffsetY = 30, AuthoredDpi = 96 });
            db.SaveChanges();

            FlowStep found = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS }, find.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = find.Id, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, found.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, found.Id);

            FlowStep missed = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE }, find.Id);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowPointId = hamburger.Id, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, missed.Id);

            FlowStep total = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = @"total: (\d+)", FlowAreaId = badge.Id, ResultExtractPattern = @"(\d+)", TimeoutMilliseconds = 10000 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = "Order is large", FlowStepReferenceId = total.Id, ConditionType = ConditionTypeEnum.GREATER_THAN, ConditionText = "100" }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Banner cleared", SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionType = ConditionTypeEnum.IS_NOT_EMPTY, FlowAreaId = header.Id }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = hamburger.Id, FlowPointEndId = origin.Id, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = CursorScrollDirectionTypeEnum.DOWN, LoopCount = 3, FlowAreaId = browser.Id }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800, WaitForMillisecondsMax = 1200 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome.exe", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH, WindowWidth = 1280, WindowHeight = 720 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.GO_TO, FlowStepReferenceId = total.Id }, null);

            FlowStep loop = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.LOOP, LoopCount = 5 }, null);
            Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = find.Id }, loop.Id);

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
