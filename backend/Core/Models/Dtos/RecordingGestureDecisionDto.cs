namespace Core.Models.Dtos
{
    /// <summary>What the tester said a gesture was for: an answer for Review to start from, or none, which leaves it for Review.</summary>
    public class RecordingGestureDecisionDto
    {
        public int PressIndex { get; set; }

        public ReviewedActionDto? Answer { get; set; }
    }
}
