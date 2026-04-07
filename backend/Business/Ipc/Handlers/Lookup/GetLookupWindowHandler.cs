using AutoMapper;
using Business.DataService.Services;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
            List<LookupItemDto> processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle))
            .Where(p => string.IsNullOrEmpty(dto.SearchText) || p.MainWindowTitle.Contains(dto.SearchText, StringComparison.OrdinalIgnoreCase))
            //.Take(req.MaxResults ?? 100)
            .Select(p => new LookupItemDto
            {
                Value = p.MainWindowTitle,
                Label = p.MainWindowTitle,
                Description = p.ProcessName,
                //ExtraData = new { ProcessId = p.Id, ProcessName = p.ProcessName }
            })
            .ToList();

            return ResultDto<LookupResponseDto>.Success(new LookupResponseDto() { Data = processes});
        }
    }
}
