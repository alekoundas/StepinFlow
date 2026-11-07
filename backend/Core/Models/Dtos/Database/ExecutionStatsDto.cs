namespace Core.Models.Dtos
{
    /// <summary>How a set of executions ended, counted by outcome, for the executions page's tiles and filters.</summary>
    public class ExecutionStatsDto
    {
        public int Total { get; set; }
        public int Executing { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public int Errored { get; set; }
        public int Inconclusive { get; set; }
        public int Stopped { get; set; }
        public int Abandoned { get; set; }

        /// <summary>When the newest failed or errored execution started. Null when none did.</summary>
        public DateTime? LastFailureOn { get; set; }

        /// <summary>The median time a completed execution took. Null when none completed.</summary>
        public int? UsualDurationMilliseconds { get; set; }
    }
}
