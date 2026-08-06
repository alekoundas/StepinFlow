using Business.Services.ScreenshotService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetLookupWindowHandler : IRequestHandler<GetLookupWindowQuery, ResultDto<LookupResponseDto>>
    {
        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupWindowQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            List<LookupItemDto> processes = AppWindowHelper.GetApplicationWindowNames()
            .Where(x => x.Contains(dto.SearchText ?? "", StringComparison.OrdinalIgnoreCase))
            .Select(x => new LookupItemDto
            {
                Value = x,
                Label = x,
                Description = x,
                //ExtraData = new { ProcessId = p.Id, ProcessName = p.ProcessName }
            })
            .ToList();

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto() { Data = processes, TotalRecords = processes.Count });
        }
    }
}
