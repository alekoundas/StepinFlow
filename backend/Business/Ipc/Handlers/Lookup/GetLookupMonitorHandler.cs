using AutoMapper;
using Business.DataService.Services;
using Business.Helpers;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLookupMonitorHandler : IRequestHandler<GetLookupMonitorQuery, ResultDto<LookupResponseDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLookupMonitorHandler(IMapper mapper, IDataService dataService, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LookupResponseDto>> Handle(GetLookupMonitorQuery request, CancellationToken ct)
        {
            List<LookupItemDto> items = ScreenHelper.GetAllMonitors().Select(monitor =>
                new LookupItemDto
                {
                    Value = monitor.DeviceId,
                    Label = monitor.FriendlyName,
                    Description = $"{monitor.Bounds.Width}×{monitor.Bounds.Height} @ ({monitor.Bounds.X}, {monitor.Bounds.Y})",
                    ExtraData = new
                    {
                        DeviceName = monitor.DeviceId,
                        IsPrimary = monitor.IsPrimary,
                        X = monitor.Bounds.X,
                        Y = monitor.Bounds.Y,
                        Width = monitor.Bounds.Width,
                        Height = monitor.Bounds.Height
                    }
                }).ToList();

            LookupResponseDto response = new LookupResponseDto { Data = items };
            return ResultDto<LookupResponseDto>.Success(response);
        }
    }
}
