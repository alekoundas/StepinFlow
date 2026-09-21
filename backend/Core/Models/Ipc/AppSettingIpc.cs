using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetAppSettingsQuery() : IRequest<ResultDto<IReadOnlyList<AppSettingDto>>>;


    // ============== COMMANDS ==============
    public record SetAppSettingCommand(SetAppSettingDto Dto) : IRequest<ResultDto<bool>>;
}
