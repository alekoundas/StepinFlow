using System.Globalization;
using System.Reflection;

using Business.FlowScript;
using Business.FlowScript.Catalogs;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Lexing;
using Business.FlowScript.Models;
using Business.FlowScript.Models.Text;
using Business.FlowScript.Scanner;
using Business.FlowScript.Text;
using Business.Tests.Fakes;
using Core.Catalogs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos.FlowScript;
using DataAccess;
using static Business.Tests.TestToken;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// GoldenFlow.approved.sflw holds every step, every mode and every field, with a value and without
    /// one. It reads, and executing it would mean nothing. Imported into a real database and exported
    /// again, it has to come back character for character.
    ///
    /// The second half of this class holds the file to that claim, so a step type, a keyword or a
    /// field added later fails here until the file shows it.
    /// </summary>
    public sealed class GoldenFlowTests : IDisposable
    {
        private const string GOLDEN = "GoldenFlow";
        private const string NO_PICTURE = "template-no-picture.png";

        private static readonly Guid CalledFlowId = Guid.Parse("3b0c1d52-6f7e-4a8b-9c0d-1e2f3a4b5c6d");

        // The columns a step's form saves that its line has no words for, so an export, an import or a
        // branch switch resets them. Each one taken into the language comes off this list.
        private static readonly Dictionary<FlowStepTypeEnum, string[]> NotInTheLanguage = new Dictionary<FlowStepTypeEnum, string[]>
        {
            [FlowStepTypeEnum.SEARCH_IMAGE] = [nameof(FlowStep.FlowStepReferenceId), nameof(FlowStep.MaxMatches), nameof(FlowStep.PollIntervalMilliseconds)],
            [FlowStepTypeEnum.SEARCH_TEXT] = [nameof(FlowStep.FlowStepReferenceId), nameof(FlowStep.OcrLanguage), nameof(FlowStep.PollIntervalMilliseconds)],
            [FlowStepTypeEnum.CURSOR_DRAG] = [nameof(FlowStep.CursorButtonType)],
            [FlowStepTypeEnum.NOTIFY] = [nameof(FlowStep.DiscordBotId)],
            [FlowStepTypeEnum.SYSTEM_COMMAND] =
            [
                nameof(FlowStep.ShellType),
                nameof(FlowStep.ShellWorkingDirectory),
                nameof(FlowStep.ShellSuccessExitCodes),
                nameof(FlowStep.ShellResultSource),
                nameof(FlowStep.ResultExtractPattern),
                nameof(FlowStep.TimeoutMilliseconds),
            ],
        };

        // A line always says these, so no row is without one.
        private static readonly Dictionary<FlowStepTypeEnum, string[]> AlwaysWritten = new Dictionary<FlowStepTypeEnum, string[]>
        {
            [FlowStepTypeEnum.CURSOR_CLICK] = [nameof(FlowStep.CursorButtonType), nameof(FlowStep.CursorButtonActionType)],
            [FlowStepTypeEnum.CURSOR_SCROLL] = [nameof(FlowStep.CursorScrollDirectionType)],
            [FlowStepTypeEnum.KEYBOARD_INPUT] = [nameof(FlowStep.KeyboardInputType)],
            [FlowStepTypeEnum.SEARCH_TEXT] = [nameof(FlowStep.ConditionType)],
            [FlowStepTypeEnum.CHECK_VALUE] = [nameof(FlowStep.ConditionType)],
        };

        private readonly TestDatabase _database = new TestDatabase();
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "sflw-golden-" + Guid.NewGuid().ToString("N")[..8]);

        public void Dispose()
        {
            _database.Dispose();

            if (Directory.Exists(_folder))
                Directory.Delete(_folder, recursive: true);
        }


        // ================================================================
        // Every character
        // ================================================================

        [Fact]
        public void It_reads_without_a_single_diagnostic()
        {
            FlowScriptSchema schema = new Scanner().Read(Golden());

            schema.Diagnostics.Select(x => $"line {x.Line}: {x.Message}").ShouldBeEmpty();
        }

        [Fact]
        public void Reading_and_printing_it_gives_back_every_character()
        {
            string printed = new Printer().Write(new Scanner().Read(Golden()));

            ApprovedFile.ShouldMatch(printed, GOLDEN);
        }

        [Fact]
        public async Task Importing_and_exporting_it_gives_back_every_character()
        {
            FlowScriptImportResultDto imported = await ImportGoldenAsync();

            ApprovedFile.ShouldMatch(await ExportAsync(imported.FlowId), GOLDEN);
        }

        [Fact]
        public async Task Importing_its_export_over_itself_changes_no_character_and_no_row()
        {
            FlowScriptImportResultDto first = await ImportGoldenAsync();
            ScriptRows before = await ScriptRows.LoadAsync(_database, first.FlowId);

            FlowScriptExportResultDto exported = await Exporter().ExportAsync(first.FlowId, Path.Combine(_folder, "out"), Ct);
            FlowScriptImportResultDto second = await Importer().ImportAsync(exported.ScriptPath, Ct);
            ScriptRows after = await ScriptRows.LoadAsync(_database, second.FlowId);

            second.FlowId.ShouldBe(first.FlowId);
            ApprovedFile.ShouldMatch(await ExportAsync(second.FlowId), GOLDEN);
            RowsThatChanged(before, after).ShouldBeEmpty();
        }

        // The one thing in it that cannot be found is the sub-flow line that names none.
        [Fact]
        public async Task Its_import_warns_only_of_the_sub_flow_it_names_nothing_for()
        {
            FlowScriptImportResultDto imported = await ImportGoldenAsync();

            imported.IsSuccess.ShouldBeTrue();
            imported.Warnings.Select(x => x.Code).Distinct().ShouldBe([nameof(DiagnosticCodeEnum.SUB_FLOW_UNKNOWN)]);
            imported.Warnings.Count.ShouldBe(2);
            imported.MissingTemplates.ShouldBe([NO_PICTURE]);
        }

        [Fact]
        public async Task Every_picture_comes_back_byte_for_byte()
        {
            FlowScriptImportResultDto imported = await ImportGoldenAsync();

            FlowScriptExportResultDto exported = await Exporter().ExportAsync(imported.FlowId, Path.Combine(_folder, "out"), Ct);

            foreach ((string name, byte[] bytes) in Pictures())
                (await File.ReadAllBytesAsync(Path.Combine(exported.TemplateFolderPath, name), Ct)).ShouldBe(bytes, name);

            File.Exists(Path.Combine(exported.TemplateFolderPath, NO_PICTURE)).ShouldBeFalse();
        }


        // ================================================================
        // What one file cannot hold: it has one kind and one main area
        // ================================================================

        [Theory]
        [InlineData("automation")]
        [InlineData("automation   sub-flow")]
        [InlineData("qa test")]
        [InlineData("qa test   sub-flow")]
        public async Task Every_kind_comes_back(string kind)
        {
            string script = $"Script Language: v1\nFlow:    Kind\nId:      7c9e6679-7425-40de-944b-e07fc1f90ae8\nKind:    {kind}\n\nSteps:\n";

            (await ImportThenRenderAsync(script)).ShouldBe(script);
        }

        [Theory]
        [InlineData("window process <[ app.exe ]>")]
        [InlineData("window process <[ app.exe ]> title is <[ App ]>   with frame   started by <[ app.exe ]>")]
        [InlineData("window process <[ chrome.exe ]>   tab <[ example.com ]>")]
        [InlineData("monitor primary")]
        [InlineData(@"monitor <[ \\.\DISPLAY2 ]>")]
        [InlineData("on screen   offset 1 2  size 3 4")]
        [InlineData("on screen   ratio 0.10 0.20  size 0.30 0.40   scales with area   at 96dpi")]
        public async Task Every_form_of_the_main_area_comes_back(string form)
        {
            string script =
                "Script Language: v1\nFlow:    Main\nId:      7c9e6679-7425-40de-944b-e07fc1f90ae9\nKind:    automation\n\n" +
                $"Areas:\n  <[ The main area ]> main   {form}\n\nSteps:\n";

            (await ImportThenRenderAsync(script)).ShouldBe(script);
        }

        // No sizes, no description, no areas, no points, no inputs, no templates and no steps.
        [Fact]
        public async Task A_flow_with_only_what_a_file_has_to_say_comes_back()
        {
            string script = "Script Language: v1\nFlow:    Bare\nId:      7c9e6679-7425-40de-944b-e07fc1f90aea\nKind:    automation\n\nSteps:\n";

            (await ImportThenRenderAsync(script)).ShouldBe(script);
        }


        // ================================================================
        // The file really does hold every case
        // ================================================================

        [Fact]
        public void Every_word_of_the_language_is_in_it()
        {
            // Units are part of a number's token. "shape" is the default and so is never printed, and
            // "automation" is covered above, since the file itself is a qa test.
            string[] notWordsHere = ["ms", "x", "shape", "automation"];

            HashSet<string> words = ScriptLineSplitter.Split(Golden())
                .SelectMany(ScriptTokenizer.Tokenize)
                .Where(x => x.Kind == ScriptTokenKindEnum.KEYWORD)
                .Select(x => x.Value)
                .ToHashSet(StringComparer.Ordinal);

            ScriptKeywordCatalog.All.Select(x => x.Text)
                .Except(notWordsHere, StringComparer.Ordinal)
                .Where(x => !words.Contains(x))
                .ShouldBeEmpty();
        }

        [Fact]
        public async Task Every_field_of_every_step_type_is_in_it_with_a_value_and_without_one()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();
            FlowStep blank = new FlowStep();
            List<string> missing = new List<string>();

            foreach (FlowStepTypeEnum type in Enum.GetValues<FlowStepTypeEnum>())
            {
                List<FlowStep> steps = rows.Steps.Where(x => x.FlowStepType == type).ToList();
                if (steps.Count == 0)
                    missing.Add($"{type}: no step");

                foreach (string field in FlowStepFieldCatalog.FieldsFor(type))
                {
                    PropertyInfo property = typeof(FlowStep).GetProperty(field)!;
                    object? nothing = property.GetValue(blank);

                    bool hasValue = steps.Any(x => !Equals(property.GetValue(x), nothing));
                    bool hasNone = steps.Any(x => Equals(property.GetValue(x), nothing));

                    if (Listed(NotInTheLanguage, type, field))
                    {
                        if (hasValue)
                            missing.Add($"{type}.{field}: the language carries it now, so it comes off the list");
                        continue;
                    }

                    if (!hasValue)
                        missing.Add($"{type}.{field}: never has a value");

                    if (!hasNone && !Listed(AlwaysWritten, type, field))
                        missing.Add($"{type}.{field}: always has a value");
                }
            }

            missing.ShouldBeEmpty();
        }

        [Fact]
        public async Task Every_value_of_every_choice_is_in_it_for_every_step_type_that_has_it()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();
            List<string> missing = new List<string>();

            foreach (FlowStepTypeEnum type in Enum.GetValues<FlowStepTypeEnum>())
            {
                foreach (string field in FlowStepFieldCatalog.FieldsFor(type).Where(x => !Listed(NotInTheLanguage, type, x)))
                {
                    PropertyInfo property = typeof(FlowStep).GetProperty(field)!;
                    Type choice = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (!choice.IsEnum)
                        continue;

                    List<object?> seen = rows.Steps.Where(x => x.FlowStepType == type).Select(x => property.GetValue(x)).ToList();

                    foreach (object value in Enum.GetValues(choice))
                    {
                        // No keyword reads a text search for every match: only an image has one.
                        if (type == FlowStepTypeEnum.SEARCH_TEXT && Equals(value, SearchModeEnum.FIND_ALL))
                            continue;

                        if (!seen.Contains(value))
                            missing.Add($"{type}.{field}: {value}");
                    }
                }
            }

            missing.ShouldBeEmpty();
        }

        [Fact]
        public async Task Every_click_button_is_in_it_with_every_action()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();

            List<(CursorButtonTypeEnum?, CursorButtonActionTypeEnum?)> seen = rows.Steps
                .Where(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_CLICK)
                .Select(x => (x.CursorButtonType, x.CursorButtonActionType))
                .ToList();

            foreach (CursorButtonTypeEnum button in Enum.GetValues<CursorButtonTypeEnum>())
            {
                foreach (CursorButtonActionTypeEnum action in Enum.GetValues<CursorButtonActionTypeEnum>())
                    seen.ShouldContain((button, action));
            }
        }

        [Fact]
        public async Task Every_text_search_mode_is_in_it_with_every_condition()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();

            List<(SearchModeEnum, ConditionTypeEnum?)> seen = rows.Steps
                .Where(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_TEXT)
                .Select(x => (x.SearchMode, x.ConditionType))
                .ToList();

            foreach (SearchModeEnum mode in new[] { SearchModeEnum.FIND_BEST, SearchModeEnum.WAIT_UNTIL_FOUND, SearchModeEnum.WAIT_UNTIL_NOT_FOUND })
            {
                foreach (ConditionTypeEnum condition in Enum.GetValues<ConditionTypeEnum>())
                    seen.ShouldContain((mode, condition));
            }
        }

        // The start and the end are each a point, a step or nothing.
        [Fact]
        public async Task Every_drag_start_is_in_it_with_every_end()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();

            List<(string, string)> seen = rows.Steps
                .Where(x => x.FlowStepType == FlowStepTypeEnum.CURSOR_DRAG)
                .Select(x => (TargetOf(x.FlowPointId, x.FlowStepReferenceId), TargetOf(x.FlowPointEndId, x.FlowStepReferenceEndId)))
                .ToList();

            foreach (string start in new[] { "point", "step", "nothing" })
            {
                foreach (string end in new[] { "point", "step", "nothing" })
                    seen.ShouldContain((start, end));
            }
        }

        [Fact]
        public async Task Every_step_type_that_can_be_disabled_is_in_it_disabled_and_enabled()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();

            foreach (FlowStepTypeEnum type in Enum.GetValues<FlowStepTypeEnum>().Where(TreeStepHelper.CanBeDisabled))
            {
                rows.Steps.ShouldContain(x => x.FlowStepType == type && x.IsDisabled, type.ToString());
                rows.Steps.ShouldContain(x => x.FlowStepType == type && !x.IsDisabled, type.ToString());
            }
        }

        // Both branches with something in, read from the file rather than from the rows an import
        // adds for the branches a file leaves out.
        [Fact]
        public void Every_step_type_that_branches_is_in_it_with_both_branches()
        {
            FlowScriptSchema schema = new Scanner().Read(Golden());

            foreach (FlowStepTypeEnum type in Enum.GetValues<FlowStepTypeEnum>().Where(TreeStepHelper.HasBranchChildren))
            {
                schema.Steps.ShouldContain(step => step.FlowStepType == type
                    && schema.Steps.Any(x => x.ParentFlowStep == step && x.FlowStepType == FlowStepTypeEnum.SUCCESS)
                    && schema.Steps.Any(x => x.ParentFlowStep == step && x.FlowStepType == FlowStepTypeEnum.FAILURE), type.ToString());
            }
        }

        [Fact]
        public async Task Every_type_of_step_a_break_can_leave_and_a_notify_can_report_on_is_in_it()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();

            List<FlowStepTypeEnum> left = TargetTypesOf(rows, FlowStepTypeEnum.BREAK);
            List<FlowStepTypeEnum> reported = TargetTypesOf(rows, FlowStepTypeEnum.NOTIFY);

            foreach (FlowStepTypeEnum type in Enum.GetValues<FlowStepTypeEnum>().Where(TreeStepHelper.HasBranchChildren))
            {
                left.ShouldContain(type);
                reported.ShouldContain(type);
            }

            left.ShouldContain(FlowStepTypeEnum.LOOP);
            left.ShouldNotContain(FlowStepTypeEnum.END_EXECUTION);
        }

        [Fact]
        public async Task Every_field_of_an_area_a_point_a_template_and_an_input_is_in_it_with_a_value_and_without_one()
        {
            ScriptRows rows = await LoadGoldenRowsAsync();
            List<FlowTemplate> pictures = rows.Templates.Select(x => x.FlowTemplate).DistinctBy(x => x.Id).ToList();

            List<string> missing = new List<string>();
            missing.AddRange(MissingEitherWay(rows.Areas, new FlowArea(), nameof(FlowArea.Name)));
            missing.AddRange(MissingEitherWay(rows.Points, new FlowPoint(), nameof(FlowPoint.Name)));
            missing.AddRange(MissingEitherWay(pictures, new FlowTemplate(), nameof(FlowTemplate.Name), nameof(FlowTemplate.Thumbnail)));
            missing.AddRange(MissingEitherWay(rows.Inputs, new FlowCsvColumn(), nameof(FlowCsvColumn.Name), nameof(FlowCsvColumn.OrderNumber)));

            missing.ShouldBeEmpty();
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static string Golden()
        {
            return ApprovedFile.Read(GOLDEN);
        }

        private static bool Listed(Dictionary<FlowStepTypeEnum, string[]> list, FlowStepTypeEnum type, string field)
        {
            return list.TryGetValue(type, out string[]? fields) && fields.Contains(field);
        }

        private static string TargetOf(int? pointId, int? stepId)
        {
            if (pointId != null)
                return "point";

            if (stepId != null)
                return "step";

            return "nothing";
        }

        private static List<FlowStepTypeEnum> TargetTypesOf(ScriptRows rows, FlowStepTypeEnum type)
        {
            return rows.Steps
                .Where(x => x.FlowStepType == type && x.FlowStepReferenceId != null)
                .Select(x => rows.Steps.Single(y => y.Id == x.FlowStepReferenceId).FlowStepType)
                .ToList();
        }

        private FlowScriptExporter Exporter()
        {
            return new FlowScriptExporter(_database, new Printer());
        }

        private FlowScriptImporter Importer()
        {
            return new FlowScriptImporter(new Scanner(), TestDataService.For(_database));
        }

        // Every picture but the one the file lets go without. They are only as much of a png as the importer reads.
        private static Dictionary<string, byte[]> Pictures()
        {
            return new Dictionary<string, byte[]>
            {
                ["Company logo.png"] = ScriptImportExportTests.Png(8, 8),
                ["template-a-file-name-longer-than-the-column.png"] = ScriptImportExportTests.Png(16, 8),
                ["template-all.png"] = ScriptImportExportTests.Png(120, 24),
                ["template-captured.png"] = ScriptImportExportTests.Png(80, 20),
                ["template-dpi.png"] = ScriptImportExportTests.Png(40, 12),
            };
        }

        // The file and its pictures on disk, as an export leaves them, with the flow it calls already here.
        private async Task<FlowScriptImportResultDto> ImportGoldenAsync()
        {
            using (AppDbContext db = _database.CreateDbContext())
            {
                db.Flows.Add(new Flow { Name = "Called flow", PublicId = CalledFlowId, IsSubFlow = true });
                db.SaveChanges();
            }

            string templateFolder = Path.Combine(_folder, "in", "Golden flow");
            Directory.CreateDirectory(templateFolder);

            foreach ((string name, byte[] bytes) in Pictures())
                await File.WriteAllBytesAsync(Path.Combine(templateFolder, name), bytes, Ct);

            string scriptPath = Path.Combine(_folder, "in", "Golden flow.sflw");
            await File.WriteAllTextAsync(scriptPath, Golden(), Ct);

            FlowScriptImportResultDto imported = await Importer().ImportAsync(scriptPath, Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));

            return imported;
        }

        private async Task<ScriptRows> LoadGoldenRowsAsync()
        {
            FlowScriptImportResultDto imported = await ImportGoldenAsync();

            return await ScriptRows.LoadAsync(_database, imported.FlowId);
        }

        private async Task<string> ExportAsync(int flowId)
        {
            FlowScriptExportResultDto exported = await Exporter().ExportAsync(flowId, Path.Combine(_folder, "out"), Ct);

            return await File.ReadAllTextAsync(exported.ScriptPath, Ct);
        }

        private async Task<string> ImportThenRenderAsync(string script)
        {
            FlowScriptImportResultDto imported = await Importer().ImportTextAsync(script, new Dictionary<string, byte[]>(), Ct);
            imported.IsSuccess.ShouldBeTrue(string.Join("; ", imported.Errors.Select(x => $"line {x.Line}: {x.Message}")));

            string rendered = await Exporter().RenderAsync(imported.FlowId, Ct);

            return rendered.ReplaceLineEndings("\n");
        }

        // Each scalar column, by reflection, that no row leaves empty or no row fills.
        private static List<string> MissingEitherWay<TRow>(IReadOnlyList<TRow> rows, TRow blank, params string[] alwaysThere)
        {
            string[] notFacts = ["Id", "CreatedOn", "UpdatedOn", "FlowId", "ParentFlowAreaId", "FlowAreaId", "TemplateImage"];
            List<string> missing = new List<string>();

            foreach (PropertyInfo property in typeof(TRow).GetProperties())
            {
                Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                bool isScalar = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(byte[]);

                if (!isScalar || notFacts.Contains(property.Name) || alwaysThere.Contains(property.Name))
                    continue;

                object? nothing = property.GetValue(blank);

                if (!rows.Any(x => !Equals(property.GetValue(x), nothing)))
                    missing.Add($"{typeof(TRow).Name}.{property.Name}: never has a value");

                if (!rows.Any(x => Equals(property.GetValue(x), nothing)))
                    missing.Add($"{typeof(TRow).Name}.{property.Name}: always has a value");
            }

            return missing;
        }

        // Every row against the row at its place in the tree, and what it points at by that row's place or name.
        private static List<string> RowsThatChanged(ScriptRows before, ScriptRows after)
        {
            List<string> changed = new List<string>();
            changed.AddRange(ScriptRows.Differences(before.Flow, after.Flow));

            Dictionary<string, FlowStep> afterByPlace = after.Steps.ToDictionary(x => PlaceOf(after, x));
            changed.AddRange(afterByPlace.Keys.Except(before.Steps.Select(x => PlaceOf(before, x))).Select(x => $"{x}: new"));

            foreach (FlowStep step in before.Steps)
            {
                string place = PlaceOf(before, step);
                FlowStep? match = afterByPlace.GetValueOrDefault(place);

                changed.AddRange(ScriptRows.Differences(step, match).Select(x => $"{place} {step.Name}: {x}"));
                if (match == null)
                    continue;

                if (before.AreaName(step.FlowAreaId) != after.AreaName(match.FlowAreaId))
                    changed.Add($"{place} {step.Name}: area");

                if (PointName(before, step.FlowPointId) != PointName(after, match.FlowPointId) || PointName(before, step.FlowPointEndId) != PointName(after, match.FlowPointEndId))
                    changed.Add($"{place} {step.Name}: point");

                if (StepPlace(before, step.FlowStepReferenceId) != StepPlace(after, match.FlowStepReferenceId) || StepPlace(before, step.FlowStepReferenceEndId) != StepPlace(after, match.FlowStepReferenceEndId))
                    changed.Add($"{place} {step.Name}: reference");

                if (step.SubFlowId != match.SubFlowId)
                    changed.Add($"{place} {step.Name}: sub-flow");
            }

            foreach (FlowArea area in before.Areas)
                changed.AddRange(ScriptRows.Differences(area, after.Areas.FirstOrDefault(x => x.Name == area.Name)).Select(x => $"{area.Name}: {x}"));

            foreach (FlowPoint point in before.Points)
                changed.AddRange(ScriptRows.Differences(point, after.Points.FirstOrDefault(x => x.Name == point.Name)).Select(x => $"{point.Name}: {x}"));

            foreach (FlowCsvColumn input in before.Inputs)
                changed.AddRange(ScriptRows.Differences(input, after.Inputs.FirstOrDefault(x => x.Name == input.Name)).Select(x => $"{input.Name}: {x}"));

            foreach (FlowStepTemplate template in before.Templates)
            {
                string place = StepPlace(before, template.FlowStepId) ?? string.Empty;
                FlowStepTemplate? match = after.Templates.FirstOrDefault(x => StepPlace(after, x.FlowStepId) == place && x.OrderNumber == template.OrderNumber);

                changed.AddRange(ScriptRows.Differences(template, match).Select(x => $"{place} {template.FlowTemplate.Name}: {x}"));
                changed.AddRange(ScriptRows.Differences(template.FlowTemplate, match?.FlowTemplate).Select(x => $"{place} {template.FlowTemplate.Name}: {x}"));
            }

            return changed;
        }

        private static string? PointName(ScriptRows rows, int? id)
        {
            return rows.Points.FirstOrDefault(x => x.Id == id)?.Name;
        }

        private static string? StepPlace(ScriptRows rows, int? id)
        {
            FlowStep? step = rows.Steps.FirstOrDefault(x => x.Id == id);
            if (step == null)
                return null;

            return PlaceOf(rows, step);
        }

        // 12.1.0: the first child of the second child of the thirteenth step.
        private static string PlaceOf(ScriptRows rows, FlowStep step)
        {
            FlowStep? parent = rows.Steps.FirstOrDefault(x => x.Id == step.ParentFlowStepId);
            if (parent == null)
                return step.OrderNumber.ToString(CultureInfo.InvariantCulture);

            return $"{PlaceOf(rows, parent)}.{step.OrderNumber}";
        }
    }
}
