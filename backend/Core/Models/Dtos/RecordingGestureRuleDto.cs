using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>A check's rule, tested against the words read on what the gesture pressed.</summary>
    public class RecordingGestureRuleDto
    {
        public int PressIndex { get; set; }
        public ConditionTypeEnum ConditionType { get; set; }
        public string? Text { get; set; }
        public string? TextEnd { get; set; }
    }
}
