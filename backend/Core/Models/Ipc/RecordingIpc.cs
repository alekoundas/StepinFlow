using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============

    /// <summary>The PNG captured for one recorded action.</summary>
    public record GetRecordingScreenshotQuery(int index) : IRequest<ResultDto<byte[]>>;


    // ============== COMMANDS ==============
    public record StartRecordingCommand() : IRequest<ResultDto<bool>>;

    /// <summary>Stops and hands back the coalesced draft, ready for the wizard.</summary>
    public record StopRecordingCommand() : IRequest<ResultDto<FlowDraftDto>>;

    public record DiscardRecordingCommand() : IRequest<ResultDto<bool>>;
}
