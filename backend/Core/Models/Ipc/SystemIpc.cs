using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    //public record GetFlowQuery(int id) : IRequest<ResultDto<FlowDto>>;
    //public record GetFlowTreeNodeQuery(int id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    //public record GetLazyFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowDto>>>;


    // ============== COMMANDS ==============
    public record SystemTakeScreenshotCommand(ScreenshotRequestDto dto) : IRequest<ResultDto<byte[]>>;
    public record SystemCaptureForOverlayCommand() : IRequest<ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>>;

}
