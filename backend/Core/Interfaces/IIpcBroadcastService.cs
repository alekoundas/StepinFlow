using Core.Enums;

namespace Core.Interfaces
{
    //-------------------------------------
    // Dependency Inversion Principle (DIP)
    //-------------------------------------

    public interface IIpcBroadcastService
    {
        ValueTask SendAsync<T>(BroadcastTypeEnum type, T payload);
    }
}
