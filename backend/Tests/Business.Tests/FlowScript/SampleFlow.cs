using System.Drawing;

using Business.FlowScript.Binding;
using Business.FlowScript.Syntax;
using Core.Enums;
using Core.Models.Database;

namespace Business.Tests.FlowScript
{
    /// <summary>
    /// A flow that uses as much of the grammar as one flow can: every area and point form, both
    /// search kinds, templates with every fact, branches, a loop, a section, a comment, cleanup
    /// under End Execution.
    /// </summary>
    public static class SampleFlow
    {
        public static BoundFlow Build()
        {
            Flow flow = new Flow { Id = 1, Name = "Login and add to cart", PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111") };

            List<FlowArea> areas =
            [
                new FlowArea { Id = 10, Name = "Browser", Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 },
                new FlowArea { Id = 11, Name = "Cart badge", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = 10, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.88f, RatioY = 0f, RatioWidth = 0.12f, RatioHeight = 0.10f },
                new FlowArea { Id = 12, Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = 10, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 0, LocationY = 0, Width = 1920, Height = 90, AuthoredDpi = 120 },
                new FlowArea { Id = 13, Name = "Game", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowAreaId = 10, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.1f, RatioY = 0.2f, RatioWidth = 0.8f, RatioHeight = 0.7f, ScalesWith = ScalesWithEnum.AREA },
                new FlowArea { Id = 14, Name = "Screen", Type = FlowAreaTypeEnum.MONITOR },
                new FlowArea { Id = 15, Name = "Right screen", Type = FlowAreaTypeEnum.MONITOR, MonitorDeviceName = @"\\.\DISPLAY2", AuthoredDpi = 96 },
                new FlowArea { Id = 16, Name = "Box", Type = FlowAreaTypeEnum.CUSTOM, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = -10, LocationY = 20, Width = 300, Height = 200 },
            ];

            List<FlowPoint> points =
            [
                new FlowPoint { Id = 20, Name = "Hamburger", FlowAreaId = 10, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.95f, RatioY = 0.05f },
                new FlowPoint { Id = 21, Name = "Origin", OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 12, LocationY = -12 },
                new FlowPoint { Id = 22, Name = "Menu", FlowAreaId = 10, OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 40, LocationY = 8, AuthoredDpi = 120 },
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

            List<FlowStep> steps = new List<FlowStep>();

            FlowStep Step(FlowStep step, int? parent)
            {
                step.RootId = 1;
                step.OrderNumber = steps.Count;
                step.ParentFlowStepId = parent;
                step.FlowId = parent == null ? 1 : null;
                steps.Add(step);
                return step;
            }

            Step(new FlowStep { Id = 100, FlowStepType = FlowStepTypeEnum.MARKER, Name = "Sign in" }, null);
            Step(new FlowStep { Id = 101, FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe https://www.saucedemo.com", CodeComment = "A fresh profile every time." }, null);

            Step(new FlowStep { Id = 102, FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE, Name = "Find username field", SearchMode = SearchModeEnum.FIND_BEST, FlowAreaId = 10, TemplateMatchMode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS }, null);
            FlowStep found = Step(new FlowStep { Id = 103, FlowStepType = FlowStepTypeEnum.SUCCESS }, 102);
            Step(new FlowStep { Id = 104, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowStepReferenceId = 102, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, found.Id);
            Step(new FlowStep { Id = 105, FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, found.Id);
            FlowStep missed = Step(new FlowStep { Id = 106, FlowStepType = FlowStepTypeEnum.FAILURE }, 102);
            Step(new FlowStep { Id = 107, FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, FlowPointId = 20, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, missed.Id);

            Step(new FlowStep { Id = 110, FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = @"total: (\d+)", FlowAreaId = 11, ResultExtractPattern = @"(\d+)", TimeoutMilliseconds = 10000 }, null);
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

            return new BoundFlow
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
                TemplatesByStepId = new Dictionary<int, IReadOnlyList<ScriptTemplate>>
                {
                    [102] =
                    [
                        new ScriptTemplate { FileName = "username-field.png", Accuracy = 0.97f, IsRequired = true, ClickOffset = new Point(60, 12), AuthoredFlowAreaWidth = 1920, AuthoredFlowAreaHeight = 1080, AuthoredDpi = 120 },
                        new ScriptTemplate { FileName = "username-alt.png", Accuracy = 0.9f, ClickOffset = new Point(-4, 10), AuthoredDpi = 96 },
                    ],
                },
                SubFlowPathsById = new Dictionary<int, string>(),
            };
        }
    }
}
