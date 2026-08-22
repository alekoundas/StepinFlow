using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetDiscordBotQuery(int id) : IRequest<ResultDto<DiscordBotDto>>;
    public record GetLazyDiscordBotQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<DiscordBotDto>>>;


    // ============== COMMANDS ==============
    public record CreateDiscordBotCommand(DiscordBotDto dto) : IRequest<ResultDto<int>>;
    public record UpdateDiscordBotCommand(DiscordBotDto dto) : IRequest<ResultDto<DiscordBotDto>>;
    public record DeleteDiscordBotCommand(int id) : IRequest<ResultDto<bool>>;

    /// <summary>
    /// Sends one message now, bypassing the throttle. A diagnostic click is not a flood, and a bot
    /// set to 300 seconds would otherwise be untestable.
    /// </summary>
    public record TestDiscordBotCommand(TestDiscordBotDto dto) : IRequest<ResultDto<bool>>;
}
