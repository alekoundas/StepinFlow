namespace Core.Enums
{
    public enum ExecutionStatusEnum
    {
        RUNNING,
        COMPLETED,
        STOPPED,
        ERRORED,

        /// <summary>
        /// Left RUNNING by a process that never came back. Set by a sweep at startup
        /// </summary>
        ABANDONED,
    }
}
