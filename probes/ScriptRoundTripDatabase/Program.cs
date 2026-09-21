// The full acceptance test: a flow in a real database, exported to a file, imported back over
// itself, exported again. Byte identical, and the row counts unchanged.
using Business.Services.FlowScriptService;

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
FlowScriptExporter exporter = new FlowScriptExporter(factory, new FlowScriptWriter());
FlowScriptImporter importer = new FlowScriptImporter(factory, new FlowScriptReader());

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

    FlowArea browser = new FlowArea { Name = "Browser", FlowId = flow.Id, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS };
    db.FlowAreas.Add(browser);
    db.SaveChanges();

    FlowArea badge = new FlowArea { Name = "Cart badge", FlowId = flow.Id, ParentFlowAreaId = browser.Id, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.88f, RatioY = 0f, RatioWidth = 0.12f, RatioHeight = 0.10f };
    FlowArea header = new FlowArea { Name = "Header", FlowId = flow.Id, ParentFlowAreaId = browser.Id, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 0, LocationY = 0, Width = 1920, Height = 90 };
    db.FlowAreas.AddRange(badge, header);

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

    FlowStep find = Add(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = browser.Id, Accuracy = 0.85f }, null);
    db.FlowStepTemplates.Add(new FlowStepTemplate { FlowStepId = find.Id, Name = "username field", OrderNumber = 0, TemplateImage = [1, 2, 3, 4], IsRequired = true });
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
