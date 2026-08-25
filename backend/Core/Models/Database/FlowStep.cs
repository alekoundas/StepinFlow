using Core.Enums;
using System.Collections.ObjectModel;

namespace Core.Models.Database
{
    public class FlowStep : BaseDbModel
    {
        public string Name { get; set; } = string.Empty;
        public FlowStepTypeEnum FlowStepType { get; set; }
        public int OrderNumber { get; set; }


        // WAIT
        //
        public int WaitForMilliseconds { get; set; }
        public int WaitForMillisecondsMax { get; set; }


        // LOOP, CURSOR_SCROLL (scroll notch count)
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }


        // IMAGE_SEARCH, READ_TEXT
        public SearchModeEnum SearchMode { get; set; }

        // Defaults for the step's templates, each of which may override them.
        public TemplateMatchModeEnum TemplateMatchMode { get; set; } = TemplateMatchModeEnum.CCoeffNormed;
        public float Accuracy { get; set; } = 0.8f;

        public int MaxMatches { get; set; } = 20;

        // Waiting modes only. 0 timeout waits forever.
        public int PollIntervalMilliseconds { get; set; } = 500;
        public int TimeoutMilliseconds { get; set; }


        // SYSTEM_COMMAND
        public RunCommandShellEnum RunCommandShell { get; set; }
        public RunCommandPresetEnum RunCommandPreset { get; set; }

        /// <summary>The preset's single parameter. Ignored by CUSTOM, which uses RunCommand.</summary>
        public string RunCommandPresetValue { get; set; } = string.Empty;
        public string RunCommand { get; set; } = string.Empty;
        public string RunCommandWorkingDirectory { get; set; } = string.Empty;

        /// <summary>Comma separated. Anything else runs the Failure children.</summary>
        public string SuccessExitCodes { get; set; } = "0";
        public ResultSourceEnum ResultSource { get; set; }


        // SYSTEM_ACTION
        public SystemActionTypeEnum SystemActionType { get; set; }


        // SYSTEM_COMMAND, READ_TEXT
        /// <summary>Regex, first capture group. Empty keeps the whole text.</summary>
        public string ResultExtractPattern { get; set; } = string.Empty;


        // READ_TEXT
        public string OcrLanguage { get; set; } = string.Empty;


        // CHECK_VALUE, READ_TEXT (the text being looked for)
        public string ConditionText { get; set; } = string.Empty;
        public ConditionTypeEnum? ConditionType { get; set; }

        /// <summary>Upper bound of BETWEEN, unused by every other condition.</summary>
        public string ConditionTextEnd { get; set; } = string.Empty;


        // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
        public string ProcessName { get; set; } = string.Empty;
        public string TitlePattern { get; set; } = string.Empty;
        public TitleMatchModeEnum TitleMatchMode { get; set; }

        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }


        // KYEBOARD_INPUT
        public string KeyboardInputText { get; set; } = string.Empty;
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }


        // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
        //
        // Point source per point:
        //   IsPointCustom = true  -> FlowPointId          (reusable named point on the Flow)
        //   IsPointCustom = false -> FlowStepReferenceId      (result of an ancestor IMAGE_SEARCH / READ_TEXT)
        // The "End" variants are the same thing for the drop point of CURSOR_DRAG.
        public bool IsPointCustom { get; set; }
        public bool IsPointEndCustom { get; set; }
        public CursorButtonTypeEnum? CursorButtonType { get; set; }
        public CursorButtonActionTypeEnum? CursorButtonActionType { get; set; }
        public CursorScrollDirectionTypeEnum? CursorScrollDirectionType { get; set; }



        // Keep the root Flow or SubFlow id for easier and faster queries
        public int RootId { get; set; }

        // Flow
        public int? FlowId { get; set; }
        public Flow? Flow { get; set; }


        // SUB_FLOW: the flow this step runs. Ownership is FlowId; this is a reference, and a
        // deleted target clears it rather than taking this step with it.
        public int? InvokedFlowId { get; set; }
        public Flow? InvokedFlow { get; set; }


        // Notify
        public int? DiscordBotId { get; set; }
        public DiscordBot? DiscordBot { get; set; }

        public string NotifyMessage { get; set; } = string.Empty;


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
        public IEnumerable<FlowStepImage> FlowStepImages { get; set; } = new Collection<FlowStepImage>();
    }
}
