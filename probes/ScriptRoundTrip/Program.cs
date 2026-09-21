// The acceptance test PLAN.md asks for: export, import, export again, byte identical.
using Business.Services.FlowScriptService;

using Core.Enums;
using Core.Models.Database;

FlowScriptWriter writer = new FlowScriptWriter();
FlowScriptReader reader = new FlowScriptReader();

string first = writer.Write(Sample());
Console.WriteLine("---------------- first export ----------------");
Console.WriteLine(first);

FlowScriptDocument document = reader.Read(first);

if (document.Errors.Count > 0)
{
    Console.WriteLine("---------------- PARSE ERRORS ----------------");
    foreach (FlowScriptError e in document.Errors)
        Console.WriteLine($"  line {e.Line} col {e.Column}: {e.Message}");
    return 1;
}

List<FlowScriptError> resolveErrors = new List<FlowScriptError>();
FlowScriptSource resolved = FlowScriptResolver.Resolve(document, resolveErrors);

if (resolveErrors.Count > 0)
{
    Console.WriteLine("---------------- RESOLVE ERRORS ----------------");
    foreach (FlowScriptError e in resolveErrors)
        Console.WriteLine($"  line {e.Line}: {e.Message}");
    return 1;
}

string second = writer.Write(resolved);

if (first == second)
{
    Console.WriteLine("ROUND TRIP IS BYTE IDENTICAL");
    return 0;
}

Console.WriteLine("---------------- DIFFERS ----------------");
string[] a = first.ReplaceLineEndings("\n").Split('\n');
string[] b = second.ReplaceLineEndings("\n").Split('\n');

for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
{
    string left = i < a.Length ? a[i] : "<missing>";
    string right = i < b.Length ? b[i] : "<missing>";

    if (left != right)
    {
        Console.WriteLine($"  line {i + 1}");
        Console.WriteLine($"    out: {left}");
        Console.WriteLine($"    in : {right}");
    }
}

return 1;


// A flow that uses as much of the grammar as one flow can: both search kinds, every placement
// form, branches, a loop, a section, a comment, cleanup under End Execution.
static FlowScriptSource Sample()
{
    Flow flow = new Flow { Id = 1, Name = "Login and add to cart", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111") };

    List<FlowArea> areas =
    [
        new FlowArea { Id = 10, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS },
        new FlowArea { Id = 11, Name = "Cart badge", ParentFlowAreaId = 10, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.88f, RatioY = 0f, RatioWidth = 0.12f, RatioHeight = 0.10f },
        new FlowArea { Id = 12, Name = "Header", ParentFlowAreaId = 10, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 0, LocationY = 0, Width = 1920, Height = 90 },
    ];

    List<FlowPoint> points =
    [
        new FlowPoint { Id = 20, Name = "Hamburger", FlowAreaId = 10, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.95f, RatioY = 0.05f },
        new FlowPoint { Id = 21, Name = "Origin", OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 12, LocationY = -12 },
    ];

    List<FlowCsvColumn> inputs =
    [
        new FlowCsvColumn { Id = 30, Name = "username", OrderNumber = 0 },
        new FlowCsvColumn { Id = 31, Name = "password", OrderNumber = 1, IsSecret = true },
    ];

    List<FlowViewport> viewports =
    [
        new FlowViewport { Id = 40, Width = 1920, Height = 1080, OrderNumber = 0 },
        new FlowViewport { Id = 41, Width = 390, Height = 844, OrderNumber = 1 },
    ];

    int order = 0;
    List<FlowStep> steps = [];

    FlowStep Step(FlowStep s, int? parent)
    {
        s.RootId = 1;
        s.OrderNumber = order++;
        s.ParentFlowStepId = parent;
        s.FlowId = parent == null ? 1 : null;
        steps.Add(s);
        return s;
    }

    Step(new FlowStep { Id = 100, FlowStepType = FlowStepTypeEnum.MARKER, Name = "Sign in" }, null);

    Step(new FlowStep
    {
        Id = 101,
        FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND,
        RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP,
        RunCommandValue = "chrome.exe https://www.saucedemo.com",
        CodeComment = "A fresh profile every time.",
    }, null);

    Step(new FlowStep { Id = 102, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = 10, Accuracy = 0.85f }, null);
    FlowStep okA = Step(new FlowStep { Id = 103, FlowStepType = FlowStepTypeEnum.SUCCESS }, 102);
    Step(new FlowStep { Id = 104, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = 102, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, okA.Id);
    Step(new FlowStep { Id = 105, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, okA.Id);
    FlowStep badA = Step(new FlowStep { Id = 106, FlowStepType = FlowStepTypeEnum.FAILURE }, 102);
    Step(new FlowStep { Id = 107, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowPointId = 20, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, badA.Id);

    Step(new FlowStep { Id = 110, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = "total: (\\d+)", FlowAreaId = 11, ResultExtractPattern = "(\\d+)", TimeoutMilliseconds = 10000 }, null);
    Step(new FlowStep { Id = 111, FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = "Order is large", FlowStepReferenceId = 110, ConditionType = ConditionTypeEnum.GREATER_THAN, ConditionText = "100" }, null);
    Step(new FlowStep { Id = 112, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Banner cleared", SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionType = ConditionTypeEnum.IS_NOT_EMPTY, FlowAreaId = 12 }, null);
    Step(new FlowStep { Id = 113, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "In range", SearchMode = SearchModeEnum.FIND_BEST, ConditionType = ConditionTypeEnum.BETWEEN, ConditionText = "1", ConditionTextEnd = "9", FlowAreaId = 12 }, null);

    Step(new FlowStep { Id = 120, FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPointId = 21 }, null);
    Step(new FlowStep { Id = 121, FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPointId = 20, FlowPointEndId = 21, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON }, null);
    Step(new FlowStep { Id = 122, FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = CursorScrollDirectionTypeEnum.DOWN, LoopCount = 3, FlowAreaId = 10 }, null);
    Step(new FlowStep { Id = 123, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Ctrl+C" }, null);
    Step(new FlowStep { Id = 124, FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800, WaitForMillisecondsMax = 1200 }, null);
    Step(new FlowStep { Id = 125, FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = SystemActionTypeEnum.LOCK_WORKSTATION }, null);
    Step(new FlowStep { Id = 126, FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome.exe", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH, WindowWidth = 1280, WindowHeight = 720 }, null);
    Step(new FlowStep { Id = 127, FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE, ProcessName = "chrome.exe", FlowPointId = 21 }, null);
    Step(new FlowStep { Id = 128, FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, ProcessName = "chrome.exe" }, null);
    Step(new FlowStep { Id = 129, FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "done" }, null);
    Step(new FlowStep { Id = 130, FlowStepType = FlowStepTypeEnum.GO_TO, FlowStepReferenceId = 110 }, null);

    FlowStep loop = Step(new FlowStep { Id = 140, FlowStepType = FlowStepTypeEnum.LOOP, LoopCount = 5 }, null);
    Step(new FlowStep { Id = 141, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = 102 }, loop.Id);

    FlowStep end = Step(new FlowStep { Id = 150, FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = false, Message = "did not reach the products page" }, null);
    Step(new FlowStep { Id = 151, FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.KILL_PROCESS, RunCommandValue = "chrome.exe" }, end.Id);

    return new FlowScriptSource
    {
        Flow = flow,
        Areas = areas,
        Points = points,
        Inputs = inputs,
        Viewports = viewports,
        Steps = steps,
        AreaNamesById = areas.ToDictionary(x => x.Id, x => x.Name),
        PointNamesById = points.ToDictionary(x => x.Id, x => x.Name),
        StepNamesById = steps.Where(x => !string.IsNullOrEmpty(x.Name)).ToDictionary(x => x.Id, x => x.Name),
        TemplateFileNamesByStepId = new Dictionary<int, IReadOnlyList<string>> { [102] = ["username-field.png", "username-alt.png"] },
        SubFlowPathsById = new Dictionary<int, string>(),
    };
}
