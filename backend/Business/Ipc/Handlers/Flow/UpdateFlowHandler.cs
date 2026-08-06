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

            SyncFlowSearchAreas(dbContext, existingFlow, request.dto.FlowSearchAreas);
            SyncFlowLocations(dbContext, existingFlow, request.dto.FlowLocations);

            await dbContext.SaveChangesAsync(ct);

            FlowDto updatedDto = _mapper.Map<FlowDto>(existingFlow);
            return ResultDto<FlowDto>.Success(updatedDto);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Children are matched by Id and updated in place. Letting AutoMapper assign the whole
        // collection would delete and re-insert every row on each save, and any FlowStep pointing
        // at a search area or location would lose its reference.
        private static void SyncFlowSearchAreas(AppDbContext dbContext, Flow flow, IEnumerable<FlowSearchAreaDto> dtos)
        {
            List<FlowSearchArea> existing = flow.FlowSearchAreas.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowSearchArea removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowSearchAreas.Remove(removed);

            foreach (FlowSearchAreaDto dto in dtos)
            {
                FlowSearchArea? flowSearchArea = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (flowSearchArea == null)
                {
                    flowSearchArea = new FlowSearchArea { FlowId = flow.Id };
                    dbContext.FlowSearchAreas.Add(flowSearchArea);
                }

                flowSearchArea.Name = dto.Name;
                flowSearchArea.Type = dto.Type;
                flowSearchArea.AppWindowName = dto.AppWindowName;
                flowSearchArea.MonitorUniqueId = dto.MonitorUniqueId;
                flowSearchArea.LocationX = dto.LocationX;
                flowSearchArea.LocationY = dto.LocationY;
                flowSearchArea.Width = dto.Width;
                flowSearchArea.Height = dto.Height;
            }
        }

        private static void SyncFlowLocations(AppDbContext dbContext, Flow flow, IEnumerable<FlowLocationDto> dtos)
        {
            List<FlowLocation> existing = flow.FlowLocations.ToList();
            HashSet<int> keptIds = dtos.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

            foreach (FlowLocation removed in existing.Where(x => !keptIds.Contains(x.Id)))
                dbContext.FlowLocations.Remove(removed);

            foreach (FlowLocationDto dto in dtos)
            {
                FlowLocation? flowLocation = dto.Id > 0 ? existing.FirstOrDefault(x => x.Id == dto.Id) : null;

                if (flowLocation == null)
                {
                    flowLocation = new FlowLocation { FlowId = flow.Id };
                    dbContext.FlowLocations.Add(flowLocation);
                }

                flowLocation.Name = dto.Name;
                flowLocation.LocationX = dto.LocationX;
                flowLocation.LocationY = dto.LocationY;
            }
        }
    }
}
