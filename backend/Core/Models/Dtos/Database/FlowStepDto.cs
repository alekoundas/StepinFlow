
using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Dtos
{
    public  class FlowStepDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }
        public int OrderNumber { get; set; }


        // WAIT
        public int WaitForMilliseconds { get; set; }


        // LOOP, CURSOR_SCROLL (scroll notch count)
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }


        // IMAGE_SEARCH, READ_TEXT
        public SearchModeEnum SearchMode { get; set; }

        public TemplateMatchModeEnum TemplateMatchMode { get; set; } = TemplateMatchModeEnum.CCoeffNormed;
        public float Accuracy { get; set; } = 0.8f;
        public int MaxMatches { get; set; } = 20;
        public int PollIntervalMilliseconds { get; set; } = 500;
        public int TimeoutMilliseconds { get; set; }


        // SYSTEM_COMMAND
        public RunCommandShellEnum RunCommandShell { get; set; }
        public RunCommandPresetEnum RunCommandPreset { get; set; }
        public string RunCommandPresetValue { get; set; } = string.Empty;
        public string RunCommand { get; set; } = string.Empty;
        public string RunCommandWorkingDirectory { get; set; } = string.Empty;
        public string SuccessExitCodes { get; set; } = "0";
        public ResultSourceEnum ResultSource { get; set; }


        // SYSTEM_ACTION
        public SystemActionTypeEnum SystemActionType { get; set; }


        // SYSTEM_COMMAND, READ_TEXT
        public string ResultExtractPattern { get; set; } = string.Empty;


        // READ_TEXT
        public string OcrLanguage { get; set; } = string.Empty;


        // CHECK_VALUE, READ_TEXT (the text being looked for)
        public string ConditionText { get; set; } = string.Empty;
        public string ConditionTextEnd { get; set; } = string.Empty;
        public ConditionTypeEnum? ConditionType { get; set; }


        // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }


        // KYEBOARD_INPUT
        public string KeyboardInputText { get; set; } = string.Empty;
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }


        // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
        public bool IsPointCustom { get; set; }
        public bool IsPointEndCustom { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public CursorButtonActionTypeEnum? CursorButtonActionType { get; set; }
        public CursorScrollDirectionTypeEnum? CursorScrollDirectionType { get; set; }


        // NOTIFICATION_EMAIL
        // TODO


        // Keep the root Flow or SubFlow id for easier and faster queries
        public int RootId { get; set; }


        // Flow
        public int? FlowId { get; set; }
        public FlowDto? Flow { get; set; }


        // SUB_FLOW: the flow this step runs.
        public int? InvokedFlowId { get; set; }
        public FlowDto? InvokedFlow { get; set; }


        // FlowArea
        public int? FlowAreaId { get; set; }
        public FlowAreaDto? FlowArea { get; set; }


        // FlowPoint (start / end point)
        public int? FlowPointId { get; set; }
        public FlowPointDto? FlowPoint { get; set; }

        public int? FlowPointEndId { get; set; }
        public FlowPointDto? FlowPointEnd { get; set; }


        // Parent FlowStep
        public int? ParentFlowStepId { get; set; }
        public FlowStepDto? ParentFlowStep { get; set; }


        // General FlowStep reference for multiple types (start / end point)
        public int? FlowStepReferenceId { get; set; }
        public FlowStepDto? FlowStepReference { get; set; }

        public int? FlowStepReferenceEndId { get; set; }
        public FlowStepDto? FlowStepReferenceEnd { get; set; }

        public IEnumerable<FlowStepDto> ChildrenFlowSteps { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowStepDto> FlowStepReferences { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowStepDto> FlowStepReferencesEnd { get; set; } = new Collection<FlowStepDto>();
        public IEnumerable<FlowStepImageDto> FlowStepImages { get; set; } = new Collection<FlowStepImageDto>();
    }
}
