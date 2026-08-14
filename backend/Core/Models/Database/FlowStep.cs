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
        public int WaitForMilliseconds { get; set; }


        // LOOP, CURSOR_SCROLL (scroll notch count)
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }


        // IMAGE_SEARCH, TEXT_SEARCH
        public ImageSearchModeEnum ImageSearchMode { get; set; }

        /// <summary>Run the Success children once per match instead of once for the best match.</summary>
        public bool LoopOnMultipleFindings { get; set; }

        // Defaults for the step's templates, each of which may override them.
        public TemplateMatchModeEnum TemplateMatchMode { get; set; } = TemplateMatchModeEnum.CCoeffNormed;
        public float Accuracy { get; set; } = 0.8f;

        /// <summary>Safety cap so a low threshold cannot return thousands of hits.</summary>
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


        // SYSTEM_COMMAND, TEXT_SEARCH
        // Named here, referenced as {{name}} by later steps. Empty means the result is dropped.
        public string ResultVariableName { get; set; } = string.Empty;

        /// <summary>Regex, first capture group. Empty keeps the whole text.</summary>
        public string ResultExtractPattern { get; set; } = string.Empty;


        // TEXT_SEARCH
        public string OcrLanguage { get; set; } = string.Empty;


        // VARIABLE_CONDITION, TEXT_SEARCH (the text being looked for)
        public string ConditionText { get; set; } = string.Empty;
        public ConditionTypeEnum? ConditionType { get; set; }


        // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
        //
        // The window itself is a FlowArea of type APPLICATION, and RELOCATE moves it to a
        // FlowPoint, so both survive being run on another machine.
        public int WindowHeight { get; set; }
        public int WindowWidth { get; set; }


        // KYEBOARD_INPUT
        public string KeyboardInputText { get; set; } = string.Empty;
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }


        // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
        //
        // Point source per point:
        //   IsPointCustom = true  -> FlowPointId          (reusable named point on the Flow)
        //   IsPointCustom = false -> FlowStepReferenceId      (result of an ancestor IMAGE_SEARCH / TEXT_SEARCH)
        // The "End" variants are the same thing for the drop point of CURSOR_DRAG.
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
        public Flow? Flow { get; set; }


        // Sub Flow
        public int? SubFlowId { get; set; }
        public SubFlow? SubFlow { get; set; }


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
