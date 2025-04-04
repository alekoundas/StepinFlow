﻿using CommunityToolkit.Mvvm.ComponentModel;
using Model.Enums;
using System.Collections.ObjectModel;

namespace Model.Models
{
    public partial class Flow : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        public string _name = "";

        [ObservableProperty]
        public FlowTypesEnum _type = FlowTypesEnum.NO_SELECTION;

        [ObservableProperty]
        public bool _isSelected = true;

        [ObservableProperty]
        public bool _isExpanded = true;

        [ObservableProperty]
        public int _orderingNum;

        public int FlowParameterId { get; set; }
        public virtual FlowParameter FlowParameter { get; set; } = new FlowParameter();

        public int FlowStepId { get; set; }
        public virtual FlowStep FlowStep { get; set; } = new FlowStep();
        
        //Only used when Flow has IsReferenced = false on flowStep
        public int? ParentSubFlowStepId { get; set; }
        public virtual FlowStep? ParentSubFlowStep { get; set; }

        public virtual ObservableCollection<FlowStep> SubFlowSteps { get; set; } = new ObservableCollection<FlowStep>();
        public virtual ObservableCollection<Execution> Executions { get; set; } = new ObservableCollection<Execution>();
    }
}
