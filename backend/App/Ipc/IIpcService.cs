using Core.Models.Ipc;

namespace App.Ipc
{
    public interface IIpcService
    {
        string Action { get; } // "flow.create"
        Type RequestType { get; }
        Task<IpcResponse> HandleAsync(object request, CancellationToken ct = default);
    }

}