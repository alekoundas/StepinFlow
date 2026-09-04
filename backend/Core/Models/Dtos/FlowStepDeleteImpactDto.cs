namespace Core.Models.Dtos
{
    /// <summary>
    /// What deleting one step takes with it.
    ///
    /// Neither number is on the tree node, and both change the answer to "are you sure". A step
    /// shows its direct children only, so a collapsed branch of twenty reads the same as one of
    /// three; and a step nothing sits under can still be the thing three other steps were
    /// clicking the result of.
    /// </summary>
    public class FlowStepDeleteImpactDto
    {
        /// <summary>Everything nested below it, to any depth. The database cascade takes all of it.</summary>
        public int DescendantCount { get; set; }

        /// <summary>
        /// Steps that survive the delete but read a result from one that does not. Their reference
        /// is set to null rather than removed, so the flow still runs and quietly does the wrong
        /// thing - which is why it is worth saying before rather than after.
        /// </summary>
        public int ReferencingStepCount { get; set; }
    }
}
