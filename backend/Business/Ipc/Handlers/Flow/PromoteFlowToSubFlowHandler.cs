using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Ipc;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Business.Ipc.Handlers
{
    /// <summary>
    /// Marks a flow as callable. One way on purpose: a caller's InvokedFlowId can then never
    /// point at something that has stopped being invokable, which is what removes every stale
    /// caller case from the rest of the feature.
    /// </summary>
    public class PromoteFlowToSubFlowHandler : IRequestHandler<PromoteFlowToSubFlowCommand, ResultDto<bool>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public PromoteFlowToSubFlowHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<bool>> Handle(PromoteFlowToSubFlowCommand request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            Flow? flow = await dbContext.Flows.FirstOrDefaultAsync(x => x.Id == request.id, ct);
            if (flow == null)
                return ResultDto<bool>.Failure("That flow no longer exists.");

            if (flow.IsSubFlow)
                return ResultDto<bool>.Success(true);

            flow.IsSubFlow = true;
            await dbContext.SaveChangesAsync(ct);

            return ResultDto<bool>.Success(true);
        }
    }

    /// <summary>
    /// The flows that invoke this one. Shown on a sub-flow so editing it is a decision rather
    /// than a surprise for whoever depends on it.
    /// </summary>
    public class GetFlowCallersHandler : IRequestHandler<GetFlowCallersQuery, ResultDto<IReadOnlyList<LookupItemDto>>>
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GetFlowCallersHandler(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ResultDto<IReadOnlyList<LookupItemDto>>> Handle(GetFlowCallersQuery request, CancellationToken ct)
        {
            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            // RootId is the flow the step lives in, which is exactly the caller.
            List<int> callerIds = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => x.InvokedFlowId == request.id)
                .Select(x => x.RootId)
                .Distinct()
                .ToListAsync(ct);

            List<LookupItemDto> callers = await dbContext.Flows
                .AsNoTracking()
                .Where(x => callerIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemDto
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,
                    Description = x.IsSubFlow ? "sub-flow" : "flow",
                })
                .ToListAsync(ct);

            return ResultDto<IReadOnlyList<LookupItemDto>>.Success(callers);
        }
    }
}
