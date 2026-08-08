
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


        // WINDOW_RELOCATE, WINDOW_RESIZE
        public int LocationX { get; set; }
        public int LocationY { get; set; }
        public int LocationEndX { get; set; }
        public int LocationEndY { get; set; }


        // WAIT
        public int WaitForMilliseconds { get; set; }


        // LOOP, CURSOR_SCROLL (scroll notch count)
        public int LoopCount { get; set; }
        public bool IsLoopInfinite { get; set; }


        // RUN_CMD
        public string RunCommand { get; set; } = string.Empty;


        // VARIABLE_CONDITION
        public string ConditionText { get; set; } = string.Empty;
        public ConditionTypeEnum? ConditionType { get; set; }


        // WINDOW_FOCUS, WINDOW_RESIZE, WINDOW_RELOCATE
        public string WindowName { get; set; } = string.Empty;
        public int WindowHeight { get; set; } // will see if i need them
        public int WindowWidth { get; set; } // will see if i need them


        // KYEBOARD_INPUT
        public string KeyboardInputText { get; set; } = string.Empty;
        public KeyboardInputTypeEnum? KeyboardInputType { get; set; }


        // CURSOR_DRAG, CURSOR_CLICK, CURSOR_RELOCATE, CURSOR_SCROLL
        public bool IsLocationCustom { get; set; }
        public bool IsLocationEndCustom { get; set; }
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


        // Sub Flow
        public int? SubFlowId { get; set; }
        public SubFlowDto? SubFlow { get; set; }


        // FlowSearchArea
        public int? FlowSearchAreaId { get; set; }
        public FlowSearchAreaDto? FlowSearchArea { get; set; }


        // FlowLocation (start / end point)
        public int? FlowLocationId { get; set; }
        public FlowLocationDto? FlowLocation { get; set; }

        public int? FlowLocationEndId { get; set; }
        public FlowLocationDto? FlowLocationEnd { get; set; }


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
