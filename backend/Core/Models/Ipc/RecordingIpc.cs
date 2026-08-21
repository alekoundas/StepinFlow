using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============

    /// <summary>The PNG captured for one recorded action.</summary>
    public record GetRecordingScreenshotQuery(int index) : IRequest<ResultDto<byte[]>>;


    // ============== COMMANDS ==============
    public record StartRecordingCommand() : IRequest<ResultDto<bool>>;

    /// <summary>Stops and hands back the coalesced actions, ready for the wizard to ask about.</summary>
    public record StopRecordingCommand() : IRequest<ResultDto<IReadOnlyList<RecordedActionDto>>>;

    public record DiscardRecordingCommand() : IRequest<ResultDto<bool>>;
}
