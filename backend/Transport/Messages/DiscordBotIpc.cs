using Core.Models.Dtos;
using MediatR;

namespace Transport.Messages
{
    // ============== QUERIES ==============
    public record GetDiscordBotQuery(int Id) : IRequest<ResultDto<DiscordBotDto>>;
    public record GetLazyDiscordBotQuery(LazyRequestDto Dto) : IRequest<ResultDto<LazyResponseDto<DiscordBotDto>>>;


    // ============== COMMANDS ==============
    public record CreateDiscordBotCommand(DiscordBotDto Dto) : IRequest<ResultDto<int>>;
    public record UpdateDiscordBotCommand(DiscordBotDto Dto) : IRequest<ResultDto<DiscordBotDto>>;
    public record DeleteDiscordBotCommand(int Id) : IRequest<ResultDto<bool>>;

    /// <summary>
    /// Sends one message now, bypassing the throttle. A diagnostic click is not a flood, and a bot
    /// set to 300 seconds would otherwise be untestable.
    /// </summary>
    public record TestDiscordBotCommand(TestDiscordBotDto Dto) : IRequest<ResultDto<bool>>;
}
