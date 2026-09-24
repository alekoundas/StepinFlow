// The full acceptance test: a flow in a real database, exported to a file, imported back over
// itself, exported again. Byte identical - and the rows the same field by field, because bytes
// alone hid an importer that dropped whatever the printer never printed.
using System.Buffers.Binary;
using System.Reflection;

using Business.FlowScript;
using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Business.FlowScript.Text;

using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

using DataAccess;
using DataAccess.Interceptors;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .AddInterceptors(new TimestampInterceptor(TimeProvider.System))
    .Options;

using (AppDbContext create = new AppDbContext(options))
    create.Database.EnsureCreated();

Factory factory = new Factory(options);
FlowScriptExporter exporter = new FlowScriptExporter(factory, new Printer());
FlowScriptImporter importer = new FlowScriptImporter(factory, new Parser());

int flowId = Seed(options);

string folder = Path.Combine(Path.GetTempPath(), "sflw-probe-" + Guid.NewGuid().ToString("N")[..8]);
Directory.CreateDirectory(folder);

int failed = 0;
void Check(string what, bool ok, string detail)
{
    if (!ok) failed++;
    Console.WriteLine($"  {(ok ? "ok  " : "FAIL")}  {what}: {detail}");
}

// ---- export ----
FlowExportResultDto exported = await exporter.ExportAsync(flowId, folder);
string first = await File.ReadAllTextAsync(exported.ScriptPath);
Check("exported", File.Exists(exported.ScriptPath), $"{exported.ScriptPath} ({exported.TemplateCount} templates)");

// ---- import over itself ----
Rows before = await Rows.LoadAsync(options, flowId);
FlowImportResultDto imported = await importer.ImportAsync(exported.ScriptPath);

if (!imported.IsSuccess)
{
    foreach (FlowScriptErrorDto e in imported.Errors)
        Console.WriteLine($"  FAIL  line {e.Line} col {e.Column}: {e.Message}");
    return 1;
}

Check("imported", imported.IsSuccess, $"flow {imported.FlowId}, {imported.StepCount} steps, {imported.TemplateCount} templates");
Check("no template lost", imported.MissingTemplates.Count == 0, string.Join(", ", imported.MissingTemplates) is { Length: > 0 } m ? m : "none");
Check("replaced rather than added", imported.FlowId == flowId, $"same flow id {imported.FlowId}");

// ---- the rows, not only the bytes ----
Rows after = await Rows.LoadAsync(options, imported.FlowId);

string[] notFacts = ["Id", "CreatedOn", "UpdatedOn", "FlowId", "RootId", "ParentFlowAreaId", "FlowAreaId", "FlowStepId",
    "ParentFlowStepId", "FlowPointId", "FlowPointEndId", "FlowStepReferenceId", "FlowStepReferenceEndId", "SubFlowId"];

Check("areas", before.Areas.Count == after.Areas.Count, $"{after.Areas.Count} of {before.Areas.Count}");
foreach (FlowArea area in before.Areas)
{
    FlowArea? match = after.Areas.FirstOrDefault(x => x.Name == area.Name);
    List<string> differences = Rows.Differences(area, match, notFacts);

    if (before.AreaName(area.ParentFlowAreaId) != after.AreaName(match?.ParentFlowAreaId))
        differences.Add("parent");

    Check($"area \"{area.Name}\"", differences.Count == 0, Rows.Describe(differences));
}

foreach (FlowPoint point in before.Points)
{
    FlowPoint? match = after.Points.FirstOrDefault(x => x.Name == point.Name);
    List<string> differences = Rows.Differences(point, match, notFacts);

    if (before.AreaName(point.FlowAreaId) != after.AreaName(match?.FlowAreaId))
        differences.Add("area");

    Check($"point \"{point.Name}\"", differences.Count == 0, Rows.Describe(differences));
}

// Named after the file it was exported to, so the name is the one field a round trip may change.
Check("templates", before.Templates.Count == after.Templates.Count, $"{after.Templates.Count} of {before.Templates.Count}");
foreach (FlowStepTemplate template in before.Templates)
{
    FlowStepTemplate? match = after.Templates.FirstOrDefault(x => after.StepName(x.FlowStepId) == before.StepName(template.FlowStepId) && x.OrderNumber == template.OrderNumber);
    List<string> differences = Rows.Differences(template, match, [.. notFacts, "Name"]);

    Check($"template {template.OrderNumber} of \"{before.StepName(template.FlowStepId)}\"", differences.Count == 0, Rows.Describe(differences));
}

FlowStep searchBefore = before.Steps.First(x => x.FlowStepType == FlowStepTypeEnum.SEARCH_IMAGE);
FlowStep? searchAfter = after.Steps.FirstOrDefault(x => x.Name == searchBefore.Name);
Check("the step's match mode", searchAfter?.TemplateMatchMode == searchBefore.TemplateMatchMode, $"{searchBefore.TemplateMatchMode} -> {searchAfter?.TemplateMatchMode}");

// Not a check: what the script still does not carry about a step, so it is seen rather than guessed.
SortedSet<string> stepLosses = new SortedSet<string>(StringComparer.Ordinal);
foreach (FlowStep step in before.Steps)
{
    FlowStep? match = after.Steps.FirstOrDefault(x => x.OrderNumber == step.OrderNumber);
    foreach (string difference in Rows.Differences(step, match, notFacts))
        stepLosses.Add($"{step.FlowStepType}.{difference.Split(':')[0]}");
}

Console.WriteLine($"  info  step fields that did not survive: {Rows.Describe(stepLosses.ToList())}");

// ---- export again ----
FlowExportResultDto again = await exporter.ExportAsync(imported.FlowId, folder);
string second = await File.ReadAllTextAsync(again.ScriptPath);

Check("byte identical round trip", first == second, first == second ? "identical" : "see diff below");

if (first != second)
{
    string[] a = first.ReplaceLineEndings("\n").Split('\n');
    string[] b = second.ReplaceLineEndings("\n").Split('\n');
    for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
    {
        string l = i < a.Length ? a[i] : "<missing>";
        string r = i < b.Length ? b[i] : "<missing>";
        if (l != r)
            Console.WriteLine($"    line {i + 1}\n      out: {l}\n      in : {r}");
    }
}

// ---- a hand-written script: no Templates section, so the click is the picture's centre ----
string handFolder = Path.Combine(folder, "hand");
Directory.CreateDirectory(handFolder);
await File.WriteAllBytesAsync(Path.Combine(handFolder, "button.png"), Png(41, 20));

string hand = """
    Flow:    Hand written
    Id:      8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b112

    Steps:
    Find Image  "Find the button"   template "button.png"
    """;

FlowImportResultDto handImported = await importer.ImportTextAsync(hand, handFolder);
FlowStepTemplate? button = null;
if (handImported.IsSuccess)
{
    using AppDbContext read = new AppDbContext(options);
    button = read.FlowStepTemplates.AsNoTracking().FirstOrDefault(x => x.FlowStep.RootId == handImported.FlowId);
}

Check("no header: the click is the centre, rounded up", button?.ClickOffsetX == 21 && button.ClickOffsetY == 10, $"{button?.ClickOffsetX},{button?.ClickOffsetY} for a 41x20 png");
Check("no header: not required, SHAPE's accuracy", button?.IsRequired == false && button.Accuracy == 0.8f, $"required {button?.IsRequired}, accuracy {button?.Accuracy}");

// ---- a typo must change nothing ----
string broken = first.Replace("Find Image", "Fnid Image");
FlowImportResultDto refused = await importer.ImportTextAsync(broken, folder);
Check("a typo is refused", !refused.IsSuccess, refused.Errors.Count > 0 ? $"line {refused.Errors[0].Line}: {refused.Errors[0].Message}" : "no error given");

FlowExportResultDto afterRefusal = await exporter.ExportAsync(flowId, folder);
string third = await File.ReadAllTextAsync(afterRefusal.ScriptPath);
Check("the flow is untouched after a refusal", second == third, second == third ? "unchanged" : "MUTATED");

Directory.Delete(folder, recursive: true);

Console.WriteLine();
Console.WriteLine(failed == 0 ? "PARSER ROUND TRIPS" : $"{failed} FAILED");
return failed;


static int Seed(DbContextOptions<AppDbContext> options)
{
    using AppDbContext db = new AppDbContext(options);

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
    db.FlowPoints.AddRange(hamburger, origin);

    db.FlowViewports.AddRange(
        new FlowViewport { FlowId = flow.Id, Width = 1920, Height = 1080, OrderNumber = 0 },
        new FlowViewport { FlowId = flow.Id, Width = 390, Height = 844, OrderNumber = 1 });

    db.FlowCsvColumns.AddRange(
        new FlowCsvColumn { FlowId = flow.Id, Name = "username", OrderNumber = 0 },
        new FlowCsvColumn { FlowId = flow.Id, Name = "password", OrderNumber = 1, IsSecret = true });

    db.SaveChanges();

    int order = 0;
    FlowStep Add(FlowStep s, int? parent)
    {
        s.RootId = flow.Id;
        s.OrderNumber = order++;
        s.ParentFlowStepId = parent;
        s.FlowId = parent == null ? flow.Id : null;
        db.FlowSteps.Add(s);
        db.SaveChanges();
        return s;
    }

    Add(new FlowStep { FlowStepType = FlowStepTypeEnum.MARKER, Name = "Sign in" }, null);
    Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe https://www.saucedemo.com", CodeComment = "A fresh profile every time." }, null);

    // Two variants, only one required, so an importer that forced every template required - which
    // this one did - turns up as a changed row.
    FlowStep find = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = browser.Id, TemplateMatchMode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS }, null);
    db.FlowStepTemplates.AddRange(
        new FlowStepTemplate { FlowStepId = find.Id, Name = "username field", OrderNumber = 0, TemplateImage = Png(120, 24), IsRequired = false, Accuracy = 0.96f, ClickOffsetX = 60, ClickOffsetY = 12, AuthoredFlowAreaWidth = 1920, AuthoredFlowAreaHeight = 1080, AuthoredDpi = 120 },
        new FlowStepTemplate { FlowStepId = find.Id, Name = "username alt", OrderNumber = 1, TemplateImage = Png(80, 20), IsRequired = true, Accuracy = 0.9f, ClickOffsetX = -4, ClickOffsetY = 30, AuthoredDpi = 96 });
    db.SaveChanges();

    FlowStep ok = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS }, find.Id);
    Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = find.Id, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, ok.Id);
    Add(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, ok.Id);

    FlowStep bad = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE }, find.Id);
    Add(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowPointId = hamburger.Id, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, bad.Id);

    FlowStep total = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = "total: (\\d+)", FlowAreaId = badge.Id, ResultExtractPattern = "(\\d+)", TimeoutMilliseconds = 10000 }, null);
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

// Only as much of a png as the importer reads: the signature and an IHDR chunk carrying the size.
static byte[] Png(int width, int height)
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


// A flow's rows, loaded to be compared: the ids differ after an import, so rows are matched by
// name and anything that points at another row is compared by that row's name.
internal sealed class Rows
{
    public List<FlowArea> Areas { get; init; } = [];
    public List<FlowPoint> Points { get; init; } = [];
    public List<FlowStep> Steps { get; init; } = [];
    public List<FlowStepTemplate> Templates { get; init; } = [];

    public static async Task<Rows> LoadAsync(DbContextOptions<AppDbContext> options, int flowId)
    {
        using AppDbContext db = new AppDbContext(options);

        return new Rows
        {
            Areas = await db.FlowAreas.AsNoTracking().Where(x => x.FlowId == flowId).ToListAsync(),
            Points = await db.FlowPoints.AsNoTracking().Where(x => x.FlowId == flowId).ToListAsync(),
            Steps = await db.FlowSteps.AsNoTracking().Where(x => x.RootId == flowId).ToListAsync(),
            Templates = await db.FlowStepTemplates.AsNoTracking().Where(x => x.FlowStep.RootId == flowId).ToListAsync(),
        };
    }

    public string? AreaName(int? id)
    {
        return Areas.FirstOrDefault(x => x.Id == id)?.Name;
    }

    public string? StepName(int id)
    {
        return Steps.FirstOrDefault(x => x.Id == id)?.Name;
    }

    // Every scalar the two rows disagree on, apart from the ones named.
    public static List<string> Differences(object before, object? after, string[] ignored)
    {
        if (after == null)
            return ["missing"];

        List<string> differences = new List<string>();

        foreach (PropertyInfo property in before.GetType().GetProperties())
        {
            Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            bool isScalar = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(byte[]);

            if (!isScalar || ignored.Contains(property.Name))
                continue;

            object? left = property.GetValue(before);
            object? right = property.GetValue(after);

            bool same;
            if (left is byte[] leftBytes && right is byte[] rightBytes)
                same = leftBytes.SequenceEqual(rightBytes);
            else
                same = Equals(left, right);

            if (!same)
                differences.Add($"{property.Name}: {left} -> {right}");
        }

        return differences;
    }

    public static string Describe(List<string> differences)
    {
        if (differences.Count == 0)
            return "none";

        return string.Join("; ", differences);
    }
}


internal sealed class Factory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public Factory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }
}
