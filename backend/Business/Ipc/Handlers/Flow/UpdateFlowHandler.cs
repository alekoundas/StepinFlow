using AutoMapper;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class UpdateFlowHandler : IRequestHandler<UpdateFlowCommand, ResultDto<FlowDto>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public UpdateFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<FlowDto>> Handle(UpdateFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Flow? existingFlow = await dbContext.Flows
                .Include(x => x.FlowSearchAreas)
                .Include(x => x.FlowLocations)
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlow == null)
                return ResultDto<FlowDto>.Failure("Flow not found");

            existingFlow.Name = request.dto.Name;
            existingFlow.OrderNumber = request.dto.OrderNumber;

            // Areas first: a location can point at an area created in this same payload.
            Dictionary<int, FlowSearchArea> areasByDtoId = SyncFlowSearchAreas(dbContext, existingFlow, request.dto.FlowSearchAreas);
            SyncFlowLocations(dbContext, existingFlow, request.dto.FlowLocations, areasByDtoId);

            await dbContext.SaveChangesAsync(ct);

            FlowDto updatedDto = _mapper.Map<FlowDto>(existingFlow);
            return ResultDto<FlowDto>.Success(updatedDto);
        }


        // ================================================================
        // Private methods
        // ================================================================
        private static Dictionary<int, FlowSearchArea> SyncFlowSearchAreas(AppDbContext dbContext, Flow flow, IEnumerable<FlowSearchAreaDto> dtos)
        {
            List<FlowSearchArea> existing = flow.FlowSearchAreas.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowSearchArea removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowSearchAreas.Remove(removed);

            Dictionary<int, FlowSearchArea> byDtoId = new Dictionary<int, FlowSearchArea>();

            foreach (FlowSearchAreaDto dto in dtos)
            {
                FlowSearchArea? area = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (area == null)
                {
                    area = new FlowSearchArea { FlowId = flow.Id };
                    dbContext.FlowSearchAreas.Add(area);
                }

                area.Name = dto.Name;
                area.Type = dto.Type;

                area.SizingMode = dto.SizingMode;
                area.LocationX = dto.LocationX;
                area.LocationY = dto.LocationY;
                area.Width = dto.Width;
                area.Height = dto.Height;
                area.RatioX = dto.RatioX;
                area.RatioY = dto.RatioY;
                area.RatioWidth = dto.RatioWidth;
                area.RatioHeight = dto.RatioHeight;

                area.ProcessName = dto.ProcessName;
                area.TitlePattern = dto.TitlePattern;
                area.TitleMatchMode = dto.TitleMatchMode;
                area.InstanceIndex = dto.InstanceIndex;
                area.UseClientArea = dto.UseClientArea;

                area.BrowserType = dto.BrowserType;
                area.TabMatchValue = dto.TabMatchValue;
                area.TabMatchOn = dto.TabMatchOn;

                area.MonitorUniqueId = dto.MonitorUniqueId;

                byDtoId[dto.Id] = area;
            }

            foreach (FlowSearchAreaDto dto in dtos)
            {
                FlowSearchArea area = byDtoId[dto.Id];

                area.ParentFlowSearchArea = dto.ParentFlowSearchAreaId != null
                    && byDtoId.TryGetValue(dto.ParentFlowSearchAreaId.Value, out FlowSearchArea? parent)
                    && parent != area
                        ? parent
                        : null;

                if (area.ParentFlowSearchArea == null)
                    area.ParentFlowSearchAreaId = null;
            }

            return byDtoId;
        }

        private static void SyncFlowLocations(AppDbContext dbContext, Flow flow, IEnumerable<FlowLocationDto> dtos, Dictionary<int, FlowSearchArea> areasByDtoId)
        {
            List<FlowLocation> existing = flow.FlowLocations.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowLocation removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowLocations.Remove(removed);

            foreach (FlowLocationDto dto in dtos)
            {
                FlowLocation? location = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (location == null)
                {
                    location = new FlowLocation { FlowId = flow.Id };
                    dbContext.FlowLocations.Add(location);
                }

                location.Name = dto.Name;
                location.Anchor = dto.Anchor;
                location.OffsetMode = dto.OffsetMode;
                location.LocationX = dto.LocationX;
                location.LocationY = dto.LocationY;
                location.RatioX = dto.RatioX;
                location.RatioY = dto.RatioY;

                location.FlowSearchArea = dto.FlowSearchAreaId != null
                    && areasByDtoId.TryGetValue(dto.FlowSearchAreaId.Value, out FlowSearchArea? area)
                        ? area
                        : null;

                if (location.FlowSearchArea == null)
                    location.FlowSearchAreaId = null;
            }
        }
    }
}
