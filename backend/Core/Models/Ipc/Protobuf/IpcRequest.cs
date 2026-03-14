using ProtoBuf;

namespace Core.Models.Ipc.Protobuf
{
    [ProtoContract]
    public class IpcRequest
    {
        [ProtoMember(1)] public string Action { get; set; } = string.Empty;
        [ProtoMember(2)] public byte[] Payload { get; set; } = Array.Empty<byte>();
        [ProtoMember(3)] public string CorrelationId { get; set; } = string.Empty;
    }
}
