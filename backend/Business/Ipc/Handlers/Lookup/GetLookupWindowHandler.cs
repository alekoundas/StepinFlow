using AutoMapper;
using Business.DataService.Services;
using Business.Services.ScreenshotService;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLookupWindowHandler : IRequestHandler<GetLookupWindowQuery, ResultDto<LookupResponseDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupWindowHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupWindowQuery request, CancellationToken ct)
        {
            LookupRequestDto dto = request.dto;

            List<LookupItemDto> processes = AppWindowHelper.GetApplicationWindowNames()
            .Where(x => x.Contains(dto.SearchText??"", StringComparison.OrdinalIgnoreCase))
            .Select(x => new LookupItemDto
            {
                Value = x,
                Label = x,
                Description = x,
                //ExtraData = new { ProcessId = p.Id, ProcessName = p.ProcessName }
            })
            .ToList();

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto() { Data = processes});
        }
    }
}
