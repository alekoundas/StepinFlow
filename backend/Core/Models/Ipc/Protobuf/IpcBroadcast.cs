using Core.Enums;
using ProtoBuf;

namespace Core.Models.Ipc.Protobuf
{
    [ProtoContract]
    public class IpcBroadcast
    {
        [ProtoMember(1)] public BroadcastTypeEnum Type { get; set; } = BroadcastTypeEnum.LOG;
        [ProtoMember(3)] public byte[]? Payload { get; set; } = Array.Empty<byte>();
    }
}
