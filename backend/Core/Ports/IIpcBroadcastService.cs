using Core.Enums;

namespace Core.Ports
{

    // Dependency Inversion Principle (DIP)
    public interface IIpcBroadcastService
    {
        ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload);
    }
}
