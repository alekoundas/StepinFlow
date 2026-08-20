using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// Display only. What a tree row shows under the step name, so the tree says what a step does
    /// without opening it. Everything is optional: each step type reads the few fields it needs.
    /// </summary>
    public class TreeNodeDetailDto
    {
        // WAIT
        public int WaitForMilliseconds { get; set; }

        // LOOP, CURSOR_SCROLL
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }

        // Resolved names, so a row never shows a bare id.
        public string? AreaName { get; set; }
        public string? PointName { get; set; }
        public string? PointEndName { get; set; }
        public string? ReferenceStepName { get; set; }
        public string? ReferenceStepEndName { get; set; }
        public string? SubFlowName { get; set; }

        // Point source per point, so the row can say "the click lands here".
        public bool IsPointCustom { get; set; }
        public bool IsPointEndCustom { get; set; }

        // CURSOR
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public CursorButtonActionTypeEnum? CursorButtonActionType { get; set; }
        public CursorScrollDirectionTypeEnum? CursorScrollDirectionType { get; set; }

        // KEYBOARD_INPUT
        public string? KeyboardInputText { get; set; }
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }

        // WINDOW_RESIZE
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        // IMAGE_SEARCH, TEXT_SEARCH
        public SearchModeEnum? SearchMode { get; set; }
        public int TemplateCount { get; set; }

        /// <summary>First template only. A row shows one image and a count, never a gallery.</summary>
        public byte[]? Thumbnail { get; set; }

        // TEXT_SEARCH, VARIABLE_CONDITION
        public string? ConditionText { get; set; }
        public ConditionTypeEnum? ConditionType { get; set; }

        // SYSTEM_COMMAND
        public RunCommandShellEnum? RunCommandShell { get; set; }
        public RunCommandPresetEnum? RunCommandPreset { get; set; }
        public string? RunCommand { get; set; }

        // SYSTEM_ACTION
        public SystemActionTypeEnum? SystemActionType { get; set; }

        // SUCCESS, FAILURE, LOOP
        public int ChildCount { get; set; }
    }
}
