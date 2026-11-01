namespace Core.Models.Dtos
{
    public class ExecutionStepRangeDto
    {
        public int ExecutionId { get; set; }

        /// <summary>Both ends are included.</summary>
        public int FirstSequence { get; set; }
        public int LastSequence { get; set; }
    }
}
