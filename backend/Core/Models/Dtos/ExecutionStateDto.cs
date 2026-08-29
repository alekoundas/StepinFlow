using Core.Enums;

namespace Core.Models.Dtos
{
    /// <summary>
    /// What the engine is doing right now. Read when the page mounts, so navigating away and back
    /// picks a run up where it is rather than showing an idle screen over a running flow.
    /// </summary>
    public class ExecutionStateDto
    {
        public RunStateEnum State { get; set; }
        public bool IsRunning { get; set; }

        public int ExecutionId { get; set; }
        public int FlowId { get; set; }
    }
}
