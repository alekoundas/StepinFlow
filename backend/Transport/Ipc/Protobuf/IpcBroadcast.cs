using ProtoBuf;

namespace Transport.Ipc.Protobuf
{
    [ProtoContract]
    public class IpcBroadcast
    {
        [ProtoMember(1)] public string Type { get; set; } = "";
        [ProtoMember(2)] public byte[]? Payload { get; set; } = Array.Empty<byte>();
    }
}
