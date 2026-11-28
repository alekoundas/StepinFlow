using Transport.Ipc.Protobuf;

namespace Transport.Ipc.Models
{
    /// <summary>
    /// A request ready to be answered, and whether it waits its turn.
    /// </summary>
    public sealed class IpcCall
    {
        public bool WaitsItsTurn { get; set; }
        public Func<Task<IpcResponse>> Answer { get; set; } = null!;
    }
}
