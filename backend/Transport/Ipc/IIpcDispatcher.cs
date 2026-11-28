using Transport.Ipc.Models;
using Transport.Ipc.Protobuf;

namespace Transport.Ipc
{
    public interface IIpcDispatcher
    {
        IpcCall Prepare(IpcRequest request, CancellationToken ct);
    }
}
