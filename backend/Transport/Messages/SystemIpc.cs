using Core.Models.Dtos;
using MediatR;

namespace Transport.Messages
{
    // ============== QUERIES ==============
    //public record GetFlowQuery(int id) : IRequest<ResultDto<FlowDto>>;
    //public record GetFlowTreeNodeQuery(int id) : IRequest<ResultDto<IEnumerable<TreeNodeDto>>>;
    //public record GetLazyFlowQuery(LazyRequestDto dto) : IRequest<ResultDto<LazyResponseDto<FlowDto>>>;


    // ============== COMMANDS ==============
    public record SystemTakeScreenshotCommand(ScreenshotRequestDto Dto) : IRequest<ResultDto<byte[]>>;
    public record SystemCaptureForOverlayCommand() : IRequest<ResultDto<IReadOnlyList<ScreenshotMonitorResponseDto>>>;


    public record SystemInputRecordAllStartCommand() : IRequest<ResultDto<bool>>;
    public record SystemInputRecordAllStopCommand() : IRequest<ResultDto<bool>>;

    public record SystemInputRecordOverlayStartCommand() : IRequest<ResultDto<bool>>;
    public record SystemInputRecordOverlayStopCommand() : IRequest<ResultDto<bool>>;

    public record SystemInputRecordPointCaptureStartCommand() : IRequest<ResultDto<bool>>;
    public record SystemInputRecordPointCaptureStopCommand() : IRequest<ResultDto<bool>>;


    public record SystemInputRecordHotkeyStartCommand() : IRequest<ResultDto<bool>>;

    public record SystemInputRecordHotkeyStopCommand() : IRequest<ResultDto<bool>>;

    public record SystemMoveCursorCommand(ScreenPointDto Dto) : IRequest<ResultDto<bool>>;

    public record SystemInstallOcrLanguageCommand(string LanguageTag) : IRequest<ResultDto<OcrLanguageInstallResultDto>>;
    public record SystemOpenWindowsLanguageSettingsCommand() : IRequest<ResultDto<bool>>;
}
