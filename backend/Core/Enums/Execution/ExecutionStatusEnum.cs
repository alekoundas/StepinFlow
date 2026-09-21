namespace Core.Enums
{
    public enum ExecutionStatusEnum
    {
        RUNNING,
        COMPLETED,
        FAILED,

        /// <summary>The walk reached the end and no End Execution ever said how it went.</summary>
        INCONCLUSIVE,
        STOPPED,
        ERRORED,

        /// <summary> Left RUNNING by a process that never came back. Set by a sweep at startup. </summary>
        ABANDONED,
    }
}
