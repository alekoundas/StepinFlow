using ProtoBuf;

namespace Core.Models.Ipc.Protobuf
{
    [ProtoContract]
    public class IpcBroadcast
    {
        [ProtoMember(1)] public string Action { get; set; } = string.Empty;
        [ProtoMember(2)] public string Message { get; set; } = string.Empty;
        [ProtoMember(3)] public byte[] Payload { get; set; } = Array.Empty<byte>();
    }
}
