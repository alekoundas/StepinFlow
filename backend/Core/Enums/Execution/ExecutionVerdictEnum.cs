namespace Core.Enums
{
    /// <summary>
    /// Whether an execution proved anything, which is not the same question as how it ended.
    ///
    /// Status says whether the walk finished: a flow that fell into a failure branch on its first
    /// step and then ran out of steps is COMPLETED. This says whether the thing under test worked.
    /// </summary>
    public enum ExecutionVerdictEnum
    {
        UNKNOWN,
        PASSED,
        FAILED,
    }
}
