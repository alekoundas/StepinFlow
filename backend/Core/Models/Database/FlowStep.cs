using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowStep : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public string CodeComment { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }
        public int OrderNumber { get; set; }

        /// <summary>An execution skips it and everything under it.</summary>
        public bool IsDisabled { get; set; }


        public int WaitForMilliseconds { get; set; }
        public int WaitForMillisecondsMax { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public int PollIntervalMilliseconds { get; set; } = 200; // Waiting modes, where 0 waits forever.


        // LOOP, CURSOR_SCROLL (scroll notch count)
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }


        // SEARCH_IMAGE, SEARCH_TEXT
        public SearchModeEnum SearchMode { get; set; }
        public TemplateMatchModeEnum TemplateMatchMode { get; set; } = TemplateMatchModeEnum.SHAPE;
        public int MaxMatches { get; set; } = 20;




        // SYSTEM_COMMAND
        public ShellCommandModeEnum ShellCommandMode { get; set; }
        public ShellTypeEnum ShellType { get; set; }
        public ShellResultSourceEnum ShellResultSource { get; set; }

        public string ShellCommandValue { get; set; } = string.Empty;
        public string ShellWorkingDirectory { get; set; } = string.Empty;
        public string ShellArguments { get; set; } = string.Empty;
        public string ShellSuccessExitCodes { get; set; } = "0"; // Comma separated.


        // SYSTEM_ACTION
        public SystemActionTypeEnum SystemActionType { get; set; }


        // SYSTEM_COMMAND, SEARCH_TEXT
        public string ResultExtractPattern { get; set; } = string.Empty;


        // SEARCH_TEXT
        public string OcrLanguage { get; set; } = string.Empty;


        // SEARCH_TEXT, CHECK_VALUE (the text being looked for)
        public string ConditionText { get; set; } = string.Empty;
        public string ConditionTextEnd { get; set; } = string.Empty;
        public ConditionTypeEnum? ConditionType { get; set; }


        // WINDOW_RESIZE
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }


        // WINDOW_CLOSE: ends the window's process when it is still open once the timeout is up.
        public bool IsCloseForced { get; set; }


        // KEYBOARD_INPUT
        public string KeyboardInputText { get; set; } = string.Empty;
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }
        public KeyboardKeyActionTypeEnum? KeyboardKeyActionType { get; set; }  


        // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public CursorButtonActionTypeEnum? CursorButtonActionType { get; set; }
        public CursorScrollDirectionTypeEnum? CursorScrollDirectionType { get; set; }


        // END_EXECUTION
        public bool EndExecutionAsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;



        // Keep the root Flow or SubFlow id for easier and faster queries
        public int RootId { get; set; }

        // Flow
        public int? FlowId { get; set; }
        public Flow? Flow { get; set; }


        // SUB_FLOW
        public int? SubFlowId { get; set; }
        public Flow? SubFlow { get; set; }


        // Notify
        public int? DiscordBotId { get; set; }
        public DiscordBot? DiscordBot { get; set; }


        // FlowArea
        public int? FlowAreaId { get; set; }
        public FlowArea? FlowArea { get; set; }


        // FlowPoint (start / end point)
        public int? FlowPointId { get; set; }
        public FlowPoint? FlowPoint { get; set; }

        public int? FlowPointEndId { get; set; }
        public FlowPoint? FlowPointEnd { get; set; }


        // Parent FlowStep
        public int? ParentFlowStepId { get; set; }
        public FlowStep? ParentFlowStep { get; set; }


        // General FlowStep reference for multiple types (start / end point)
        public int? FlowStepReferenceId { get; set; }
        public FlowStep? FlowStepReference { get; set; }

        public int? FlowStepReferenceEndId { get; set; }
        public FlowStep? FlowStepReferenceEnd { get; set; }

        public IEnumerable<FlowStep> ChildrenFlowSteps { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowStep> FlowStepReferences { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowStep> FlowStepReferencesEnd { get; set; } = new Collection<FlowStep>();
        public IEnumerable<FlowStepTemplate> FlowStepTemplates { get; set; } = new Collection<FlowStepTemplate>();
    }
}
