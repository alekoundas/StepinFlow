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
                .Include(x => x.FlowAreas)
                .Include(x => x.FlowPoints)
                .FirstOrDefaultAsync(x => x.Id == request.dto.Id, ct);

            if (existingFlow == null)
                return ResultDto<FlowDto>.Failure("Flow not found");

            existingFlow.Name = request.dto.Name;
            existingFlow.Description = request.dto.Description;

            // Areas first: a location can point at an area created in this same payload.
            Dictionary<int, FlowArea> areasByDtoId = SyncFlowAreas(dbContext, existingFlow, request.dto.FlowAreas);
            SyncFlowPoints(dbContext, existingFlow, request.dto.FlowPoints, areasByDtoId);

            await dbContext.SaveChangesAsync(ct);

            FlowDto updatedDto = _mapper.Map<FlowDto>(existingFlow);
            return ResultDto<FlowDto>.Success(updatedDto);
        }


        // ================================================================
        // Private methods
        // ================================================================
        private static Dictionary<int, FlowArea> SyncFlowAreas(AppDbContext dbContext, Flow flow, IEnumerable<FlowAreaDto> dtos)
        {
            List<FlowArea> existing = flow.FlowAreas.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowArea removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowAreas.Remove(removed);

            Dictionary<int, FlowArea> byDtoId = new Dictionary<int, FlowArea>();

            foreach (FlowAreaDto dto in dtos)
            {
                FlowArea? area = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (area == null)
                {
                    area = new FlowArea { FlowId = flow.Id };
                    dbContext.FlowAreas.Add(area);
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

            foreach (FlowAreaDto dto in dtos)
            {
                FlowArea area = byDtoId[dto.Id];

                area.ParentFlowArea = dto.ParentFlowAreaId != null
                    && byDtoId.TryGetValue(dto.ParentFlowAreaId.Value, out FlowArea? parent)
                    && parent != area
                        ? parent
                        : null;

                if (area.ParentFlowArea == null)
                    area.ParentFlowAreaId = null;
            }

            return byDtoId;
        }

        private static void SyncFlowPoints(AppDbContext dbContext, Flow flow, IEnumerable<FlowPointDto> dtos, Dictionary<int, FlowArea> areasByDtoId)
        {
            List<FlowPoint> existing = flow.FlowPoints.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowPoint removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowPoints.Remove(removed);

            foreach (FlowPointDto dto in dtos)
            {
                FlowPoint? location = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (location == null)
                {
                    location = new FlowPoint { FlowId = flow.Id };
                    dbContext.FlowPoints.Add(location);
                }

                location.Name = dto.Name;
                location.OffsetMode = dto.OffsetMode;
                location.LocationX = dto.LocationX;
                location.LocationY = dto.LocationY;
                location.RatioX = dto.RatioX;
                location.RatioY = dto.RatioY;

                location.FlowArea = dto.FlowAreaId != null
                    && areasByDtoId.TryGetValue(dto.FlowAreaId.Value, out FlowArea? area)
                        ? area
                        : null;

                if (location.FlowArea == null)
                    location.FlowAreaId = null;
            }
        }
    }
}
