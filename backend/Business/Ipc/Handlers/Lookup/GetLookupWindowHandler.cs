using Business.Services.ScreenshotService;
using Core.Models.Business;
using Core.Models.Dtos;
using Core.Models.Ipc;
using MediatR;

namespace Business.Ipc.Handlers
{
    public class GetLookupWindowHandler : IRequestHandler<GetLookupWindowQuery, ResultDto<LookupResponseDto>>
    {
        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupWindowQuery request, CancellationToken ct)
        {
            string search = request.dto.SearchText ?? string.Empty;

            // Value is the process name: window titles change constantly, process names do not.
            // The title comes along in ExtraData so the form can offer it as a starting pattern.
            List<LookupItemDto> items = AppWindowHelper.GetApplicationWindows()
                .Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.ProcessName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(x => new LookupItemDto
                {
                    Value = x.ProcessName,
                    Label = x.Title,
                    Description = x.ProcessName,
                    ExtraData = new
                    {
                        ProcessName = x.ProcessName,
                        Title = x.Title,
                    },
                })
                .ToList();

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto { Data = items, TotalRecords = items.Count });
        }
    }
}
