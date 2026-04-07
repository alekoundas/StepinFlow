using Core.Models.Dtos;
using MediatR;

namespace Core.Models.Ipc
{
    // ============== QUERIES ==============
    public record GetLookupWindowQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupMonitorQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;
    public record GetLookupFlowStepQuery(LookupRequestDto dto) : IRequest<ResultDto<LookupResponseDto>>;


    // ============== COMMANDS ==============
}
