using AutoMapper;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    public class GetLazyFlowHandler : IRequestHandler<GetLazyFlowQuery, ResultDto<LazyResponseDto<FlowDto>>>
    {
        private readonly IMapper _mapper;
        private IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetLazyFlowHandler(IMapper mapper, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _mapper = mapper;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<LazyResponseDto<FlowDto>>> Handle(GetLazyFlowQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            IQueryable<Flow> query = dbContext.Flows.AsNoTracking();

            // Unset lists everything, which is what a lookup wants; the two pages always set it.
            if (request.dto.IsSubFlow is bool isSubFlow)
                query = query.Where(x => x.IsSubFlow == isSubFlow);

            // Projected, not mapped: the list wants counts, and loading three collections per
            // row to count them is the difference between one query and dozens.
            //
            // SUCCESS and FAILURE are excluded from the step count. They are created with their
            // parent, so counting them would report a flow as twice the size the user built.
            List<FlowDto> flowDtos = await query
                .OrderBy(x => x.Name)
                .Select(x => new FlowDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsSubFlow = x.IsSubFlow,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.UpdatedOn,

                    StepCount = x.FlowSteps.Count(step =>
                        step.FlowStepType != FlowStepTypeEnum.SUCCESS &&
                        step.FlowStepType != FlowStepTypeEnum.FAILURE),
                    AreaCount = x.FlowAreas.Count(),
                    PointCount = x.FlowPoints.Count(),

                    // Distinct callers, not calling steps: one flow may invoke this twice and
                    // that is still one thing depending on it.
                    CallerCount = x.IsSubFlow
                        ? dbContext.FlowSteps
                            .Where(step => step.SubFlowId == x.Id)
                            .Select(step => step.RootId)
                            .Distinct()
                            .Count()
                        : 0,
                })
                .ToListAsync(ct);
            LazyResponseDto<FlowDto> dataTableResponseDto = new LazyResponseDto<FlowDto>();
            dataTableResponseDto.Data = flowDtos;
            dataTableResponseDto.TotalRecords = flowDtos.Count;

            return ResultDto<LazyResponseDto<FlowDto>>.Success(dataTableResponseDto);
        }
    }
}
