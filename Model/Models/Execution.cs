using CommunityToolkit.Mvvm.ComponentModel;
using Model.Enums;

namespace Model.Models
{
    public partial class Execution : ObservableObject
    {
        public int Id { get; set; }

        [ObservableProperty]
        public ExecutionStatusEnum _status = ExecutionStatusEnum.DASH;

        [ObservableProperty]
        public ExecutionResultEnum _result = ExecutionResultEnum.NO_RESULT;

        [ObservableProperty]
        public DateTime? _startedOn;

        [ObservableProperty]
        public DateTime? _endedOn;

        [ObservableProperty]
        public bool _isSelected = true;

        public string ExecutionFolderDirectory = "";

        // Template properties.
        [ObservableProperty]
        public int? _loopCount;

        [ObservableProperty]
        public int? _resultLocationX;

        [ObservableProperty]
        public int? _resultLocationY;

        [ObservableProperty]
        public string? _resultImagePath;

        [ObservableProperty]
        public string? _tempResultImagePath;

        [ObservableProperty]
        public decimal _resultAccuracy = 0.00m;

        // Navigation properties.    
        public int? FlowId { get; set; }
        public virtual Flow? Flow { get; set; }

        public int? FlowStepId { get; set; }
        public virtual FlowStep? FlowStep { get; set; }

        public int? ParentExecutionId { get; set; }
        public virtual Execution? ParentExecution { get; set; }

        public int? ChildExecutionId { get; set; }
        public virtual Execution? ChildExecution { get; set; }
    }
}
