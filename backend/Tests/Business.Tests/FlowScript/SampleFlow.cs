using Business.FlowScript.Models;
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
        public static FlowScriptSchema Build()
        {
            FlowScriptSchema schema = new FlowScriptSchema
            {
                Flow = new Flow
                {
                    Name = "Login and add to cart",
                    PublicId = Guid.Parse("8f14e45f-ea2b-4c3f-9f1a-77f0d2a3b111"),
                    Description = "Logs in to Swag Labs and adds the backpack to the cart.\n\nThe cart badge shows the count.",
                },
            };

            FlowArea browser = new FlowArea { Name = "Browser", IsMain = true, Type = FlowAreaTypeEnum.APPLICATION, ProcessName = "chrome.exe", TitlePattern = "Swag Labs", TitleMatchMode = TitleMatchModeEnum.CONTAINS, ScalesWith = ScalesWithEnum.DPI, AuthoredDpi = 120 };
            FlowArea badge = new FlowArea { Name = "Cart badge", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowArea = browser, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.88f, RatioY = 0f, RatioWidth = 0.12f, RatioHeight = 0.10f };
            FlowArea header = new FlowArea { Name = "Header", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowArea = browser, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 0, LocationY = 0, Width = 1920, Height = 90, AuthoredDpi = 120 };
            FlowArea game = new FlowArea { Name = "Game", Type = FlowAreaTypeEnum.CUSTOM, ParentFlowArea = browser, SizingMode = AreaSizingModeEnum.RATIO, RatioX = 0.1f, RatioY = 0.2f, RatioWidth = 0.8f, RatioHeight = 0.7f, ScalesWith = ScalesWithEnum.AREA };
            FlowArea screen = new FlowArea { Name = "Screen", Type = FlowAreaTypeEnum.MONITOR };
            FlowArea rightScreen = new FlowArea { Name = "Right screen", Type = FlowAreaTypeEnum.MONITOR, MonitorDeviceName = @"\\.\DISPLAY2", AuthoredDpi = 96 };
            FlowArea box = new FlowArea { Name = "Box", Type = FlowAreaTypeEnum.CUSTOM, SizingMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = -10, LocationY = 20, Width = 300, Height = 200 };
            schema.Areas.AddRange([browser, badge, header, game, screen, rightScreen, box]);

            FlowPoint hamburger = new FlowPoint { Name = "Hamburger", FlowArea = browser, OffsetMode = AreaSizingModeEnum.RATIO, RatioX = 0.95f, RatioY = 0.05f };
            FlowPoint origin = new FlowPoint { Name = "Origin", OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 12, LocationY = -12 };
            FlowPoint menu = new FlowPoint { Name = "Menu", FlowArea = browser, OffsetMode = AreaSizingModeEnum.ABSOLUTE_PX, LocationX = 40, LocationY = 8, AuthoredDpi = 120 };
            schema.Points.AddRange([hamburger, origin, menu]);

            schema.Inputs.AddRange(
            [
                new FlowCsvColumn { Name = "username", OrderNumber = 0, DefaultValue = "standard_user" },
                new FlowCsvColumn { Name = "password", OrderNumber = 1, IsSecret = true },
            ]);

            schema.Viewports.AddRange(
            [
                new FlowViewport { Width = 1920, Height = 1080, OrderNumber = 0 },
                new FlowViewport { Width = 390, Height = 844, OrderNumber = 1 },
            ]);

            FlowStep Step(FlowStep step, FlowStep? parent)
            {
                step.ParentFlowStep = parent;
                step.OrderNumber = schema.Steps.Count(x => x.ParentFlowStep == parent);
                schema.Steps.Add(step);
                return step;
            }

            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.STAGE_MARKER, Name = "Sign in", CodeComment = "Everything before the products page." }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.LAUNCH_APP, RunCommandValue = "chrome.exe https://www.saucedemo.com", CodeComment = "A fresh profile every time." }, null);

            FlowStep find = Step(new FlowStep
            {
                FlowStepType = FlowStepTypeEnum.SEARCH_IMAGE,
                Name = "Find username field",
                SearchMode = SearchModeEnum.FIND_BEST,
                FlowArea = browser,
                TemplateMatchMode = TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS,
                FlowStepTemplates =
                [
                    new FlowStepTemplate { Name = "template-u9d3n.png", OrderNumber = 0, Accuracy = 0.97f, IsRequired = true, ClickOffsetX = 60, ClickOffsetY = 12, AuthoredFlowAreaWidth = 1920, AuthoredFlowAreaHeight = 1080, AuthoredDpi = 120 },
                    new FlowStepTemplate { Name = "template-w2h6r.png", OrderNumber = 1, Accuracy = 0.9f, ClickOffsetX = -4, ClickOffsetY = 10, AuthoredDpi = 96 },
                ],
            }, null);
            FlowStep found = Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SUCCESS }, find);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReference = find }, found);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.SINGLE_CLICK }, found);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.TEXT, KeyboardInputText = "{{username}}" }, found);
            FlowStep missed = Step(new FlowStep { FlowStepType = FlowStepTypeEnum.FAILURE, CodeComment = "The field is behind the menu on a narrow window." }, find);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPoint = hamburger }, missed);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK, CursorButtonType = CursorButtonTypeEnum.RIGHT_BUTTON, CursorButtonActionType = CursorButtonActionTypeEnum.DOUBLE_CLICK }, missed);

            FlowStep total = Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Read the total", SearchMode = SearchModeEnum.WAIT_UNTIL_FOUND, ConditionType = ConditionTypeEnum.MATCHES_REGEX, ConditionText = @"total: (\d+)", FlowArea = badge, ResultExtractPattern = @"(\d+)", TimeoutMilliseconds = 10000 }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CHECK_VALUE, Name = "Order is large", FlowStepReference = total, ConditionType = ConditionTypeEnum.GREATER_THAN, ConditionText = "100" }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "Banner cleared", SearchMode = SearchModeEnum.WAIT_UNTIL_NOT_FOUND, ConditionType = ConditionTypeEnum.IS_NOT_EMPTY, FlowArea = header }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SEARCH_TEXT, Name = "In range", SearchMode = SearchModeEnum.FIND_BEST, ConditionType = ConditionTypeEnum.BETWEEN, ConditionText = "1", ConditionTextEnd = "9", FlowArea = header }, null);

            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowPoint = origin }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_DRAG, FlowPoint = hamburger, FlowPointEnd = origin, CursorButtonType = CursorButtonTypeEnum.LEFT_BUTTON }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_SCROLL, CursorScrollDirectionType = CursorScrollDirectionTypeEnum.DOWN, LoopCount = 3 }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Ctrl+C" }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Shift", KeyboardKeyActionType = KeyboardKeyActionTypeEnum.HOLD }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.KEYBOARD_INPUT, KeyboardInputType = KeyboardInputTypeEnum.COMBINATION, KeyboardInputText = "Shift", KeyboardKeyActionType = KeyboardKeyActionTypeEnum.RELEASE }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.WAIT, WaitForMilliseconds = 800, WaitForMillisecondsMax = 1200 }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_ACTION, SystemActionType = SystemActionTypeEnum.LOCK_WORKSTATION }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RESIZE, ProcessName = "chrome.exe", TitlePattern = "Swag", TitleMatchMode = TitleMatchModeEnum.STARTS_WITH, WindowWidth = 1280, WindowHeight = 720 }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_RELOCATE, ProcessName = "chrome.exe", FlowPoint = origin }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.WINDOW_FOCUS, ProcessName = "chrome.exe" }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.NOTIFY, Message = "done" }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.GO_BACK, FlowStepReference = total }, null);

            FlowStep loop = Step(new FlowStep { FlowStepType = FlowStepTypeEnum.LOOP, LoopCount = 5 }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_RELOCATE, FlowStepReference = find }, loop);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.CURSOR_CLICK }, loop);

            FlowStep end = Step(new FlowStep { FlowStepType = FlowStepTypeEnum.END_EXECUTION, EndExecutionAsSuccess = false, Message = "did not reach the products page" }, null);
            Step(new FlowStep { FlowStepType = FlowStepTypeEnum.SYSTEM_COMMAND, RunCommandPreset = RunCommandPresetEnum.KILL_PROCESS, RunCommandValue = "chrome.exe" }, end);

            return schema;
        }
    }
}
