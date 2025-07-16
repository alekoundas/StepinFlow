using CommunityToolkit.Mvvm.ComponentModel;
using Model.Enums;
using System.Collections.ObjectModel;

namespace Model.Models
{
    public partial class FlowStep : ObservableObject
    {
        [ObservableProperty]
        public int id;

        [ObservableProperty]
        public string _name = string.Empty;

        [ObservableProperty]
        public string _processName = string.Empty;

        [ObservableProperty]
        public bool _isExpanded;

        [ObservableProperty]
        public bool _isSelected;

        [ObservableProperty]
        public int _orderingNum;

        [ObservableProperty]
        public FlowStepTypesEnum _type;

        [ObservableProperty]
        public TemplateMatchModesEnum? _templateMatchMode;


        // SubFlow
        [ObservableProperty]
        public bool _isSubFlowReferenced;

        // Template search
        [ObservableProperty]
        public byte[]? _templateImage = null;

        [ObservableProperty]
        public decimal _accuracy = 0.00m;

        [ObservableProperty]
        public bool _removeTemplateFromResult;

        // Cursor
        [ObservableProperty]
        public CursorActionsEnum? _cursorAction;

        [ObservableProperty]
        public CursorButtonsEnum? _cursorButton;

        [ObservableProperty]
        public CursorScrollDirectionEnum? _cursorScrollDirection;

        [ObservableProperty]
        public CursorRelocationTypesEnum? _cursorRelocationType;

        //System
        //[ObservableProperty]
        //public int _waitForHours;

        //[ObservableProperty]
        //public int _waitForMinutes;

        //[ObservableProperty]
        //public int _waitForSeconds;

        //[ObservableProperty]
        //public int _waitForMilliseconds;

        [ObservableProperty]
        public int _milliseconds;


        // Window
        [ObservableProperty]
        public int _height;

        [ObservableProperty]
        public int _width;

        // Reusable
        [ObservableProperty]
        public bool _isLoop;

        [ObservableProperty]
        public bool _isLoopInfinite;

        [ObservableProperty]
        public int _loopCount;

        [ObservableProperty]
        public int _loopMaxCount;

        [ObservableProperty]
        public TimeOnly? _loopTime;

        [ObservableProperty]
        public int _locationX;

        [ObservableProperty]
        public int _locationY;



        //Flow
        public int? FlowId { get; set; }
        public virtual Flow? Flow { get; set; }

        public int? SubFlowId { get; set; }
        public virtual Flow? SubFlow { get; set; }


        //FlowParameter
        public int? FlowParameterId { get; set; }
        public virtual FlowParameter? FlowParameter { get; set; }


        //FlowStep
        public int? ParentFlowStepId { get; set; }
        public virtual FlowStep? ParentFlowStep { get; set; }

        public int? ParentTemplateSearchFlowStepId { get; set; } // Used by GoTo, CursorMove, MultipleTemplateSearch, MultipleTemplateSearchLoop
        public virtual FlowStep? ParentTemplateSearchFlowStep { get; set; }



        public virtual ObservableCollection<Flow> SubFlows { get; set; }  = new ObservableCollection<Flow>();
        public virtual ObservableCollection<Execution> Executions { get; set; }  = new ObservableCollection<Execution>();
        public virtual ObservableCollection<FlowStep> ChildrenFlowSteps { get; set; } = new ObservableCollection<FlowStep>();
        public virtual ObservableCollection<FlowStep> ChildrenTemplateSearchFlowSteps { get; set; } = new ObservableCollection<FlowStep>();
    }
}
