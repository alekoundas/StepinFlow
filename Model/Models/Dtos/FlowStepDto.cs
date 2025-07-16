using CommunityToolkit.Mvvm.ComponentModel;
using Model.Enums;

namespace Model.Models
{
    public partial class FlowStepDto : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public int OrderingNum { get; set; }
        public FlowStepTypesEnum Type { get; set; }
        public TemplateMatchModesEnum? TemplateMatchMode { get; set; }

        // SubFlow
        public bool IsSubFlowReferenced { get; set; }

        // Template search
        public byte[]? TemplateImage { get; set; } = null;
        public decimal Accuracy { get; set; } = 0.00m;
        public bool RemoveTemplateFromResult { get; set; }

        // Cursor
        public CursorActionsEnum? CursorAction { get; set; }
        public CursorButtonsEnum? CursorButton { get; set; }
        public CursorScrollDirectionEnum? CursorScrollDirection { get; set; }
        public CursorRelocationTypesEnum? CursorRelocationType { get; set; }

        // System
        //public int WaitForHours { get; set; }
        //public int WaitForMinutes { get; set; }
        //public int WaitForSeconds { get; set; }
        //public int WaitForMilliseconds { get; set; }
        public int Milliseconds;

        // Window
        public int Height { get; set; }
        public int Width { get; set; }

        // Reusable
        public bool IsLoop { get; set; }
        public bool IsLoopInfinite { get; set; }
        public int LoopCount { get; set; }
        public int LoopMaxCount { get; set; }
        public TimeOnly? LoopTime { get; set; }
        public int LocationX { get; set; }
        public int LocationY { get; set; }


        public int? FlowId { get; set; }
        public int? SubFlowId { get; set; }
        public FlowDto? SubFlow { get; set; }

        public int? FlowParameterId { get; set; }
        public int? ParentFlowStepId { get; set; }

        public int? ParentTemplateSearchFlowStepId;

        public virtual List<FlowDto> SubFlows { get; set; } = new List<FlowDto>();
        public virtual List<FlowStepDto> ChildrenFlowSteps { get; set; } = new List<FlowStepDto>();
        public virtual List<FlowStepDto> ChildrenTemplateSearchFlowSteps { get; set; } = new List<FlowStepDto>();

        //public virtual ObservableCollection<ExecutionDto> Executions { get; set; } = new ObservableCollection<ExecutionDto>();

    }
}
