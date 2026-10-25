using Business.Executions;
using Business.Flows.DataService;
using Business.Flows.DataService.DataHandlers;
using Business.FlowScript;
using Business.FlowScript.Diagnostics;
using Business.FlowScript.Models;
using Core.Enums;
using Core.Models.Business.Git;
using Core.Models.Database;
using Core.Models.Dtos;
using Core.Models.Dtos.Repository;
using Core.Ports;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Business.Repository
{
    /// <summary>
    /// Switching branch: the folder checks the branch out, and the database follows. Only the flows
    /// whose files differ are touched: one the branch changed or added is imported by its Id, and one
    /// it does not have is hidden, never deleted, since deleting a flow deletes its executions.
    ///
    /// One transaction, so a flow that does not import leaves every flow, and the folder, as they
    /// were. Apart from <see cref="GitRepositoryService"/> for the reason <see cref="FlowRestoreService"/> is.
    /// </summary>
    public sealed class BranchSwitchService : IBranchSwitchService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly DataService _dataService;
        private readonly IFlowScriptImporter _importer;
        private readonly IGitRepositoryService _gitRepositoryService;
        private readonly IExecutionEngine _executionEngine;
        private readonly IIpcBroadcastService _broadcast;

        public BranchSwitchService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            DataService dataService,
            IFlowScriptImporter importer,
            IGitRepositoryService gitRepositoryService,
            IExecutionEngine executionEngine,
            IIpcBroadcastService broadcast)
        {
            _dbContextFactory = dbContextFactory;
            _dataService = dataService;
            _importer = importer;
            _gitRepositoryService = gitRepositoryService;
            _executionEngine = executionEngine;
            _broadcast = broadcast;
        }


        // ================================================================
        // Public methods
        // ================================================================

        public async Task<ResultDto<RepositoryBranchSwitchDto>> SwitchAsync(string branch, CancellationToken ct)
        {
            // The executing flow reads its steps from the rows a switch replaces.
            if (_executionEngine.IsRunning)
                return ResultDto<RepositoryBranchSwitchDto>.Failure("A flow is executing. Stop it, or let it finish, then switch.");

            RepositoryBranchSwitchDto progress = new RepositoryBranchSwitchDto { Branch = branch };

            ResultDto<GitBranchSwitch> switched = await _gitRepositoryService.SwitchBranchAsync(branch, (branchSwitch, token) => ApplyAsync(branchSwitch, progress, token), ct);
            if (!switched.IsSuccess)
                return ResultDto<RepositoryBranchSwitchDto>.Failure(switched.ErrorMessage ?? "The branch could not be switched to.");

            RepositoryChangedDto changed = new RepositoryChangedDto { FlowIds = progress.Flows.Where(x => x.FlowId != null).Select(x => x.FlowId!.Value).ToList() };
            await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_CHANGED, changed);

            return ResultDto<RepositoryBranchSwitchDto>.Success(progress);
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Called with the branch checked out and the folder held.
        private async Task<ResultDto<bool>> ApplyAsync(GitBranchSwitch branchSwitch, RepositoryBranchSwitchDto progress, CancellationToken ct)
        {
            // Every file read before anything is saved: one that does not read refuses the switch.
            List<FlowScriptSchema> schemas = new List<FlowScriptSchema>();
            foreach (GitFlowFiles flow in branchSwitch.Flows)
            {
                FlowScriptSchema schema = _importer.Read(flow.Script, flow.Templates);

                Diagnostic? error = schema.Diagnostics.FirstOrDefault(x => x.Severity == DiagnosticSeverityEnum.ERROR);
                if (error != null)
                    return ResultDto<bool>.Failure($"{flow.Path} on {branchSwitch.To} does not read, so nothing was switched. Line {error.Line}: {error.Message}");

                schemas.Add(schema);
            }

            schemas = CalleesFirst(schemas);

            await using AppDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

            List<RepositoryBranchSwitchFlowDto> imports = await ListAsync(dbContext, branchSwitch, schemas, progress, ct);
            await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_BRANCH_SWITCH, progress);

            await FlowDataService.HideAsync(dbContext, branchSwitch.GoneFlowIds, ct);
            foreach (RepositoryBranchSwitchFlowDto hidden in progress.Flows.Where(x => x.Change == GitChangeKindEnum.DELETED))
                hidden.IsDone = true;

            List<Guid> publicIds = schemas.Select(x => x.Flow.PublicId).ToList();
            await FlowDataService.ReleaseNamesAsync(dbContext, publicIds, ct);

            for (int i = 0; i < schemas.Count; i++)
            {
                await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_BRANCH_SWITCH, progress);

                FlowScriptSchema schema = schemas[i];
                await FlowDataService.LinkSubFlowsAsync(dbContext, schema, ct);

                ResultDto<(int FlowId, FlowValidationResultDto Validation)> replaced = await _dataService.Flow.ReplaceAsync(dbContext, schema, ct);
                if (!replaced.IsSuccess)
                    return ResultDto<bool>.Failure($"\"{schema.Flow.Name}\" on {branchSwitch.To} could not be imported, so nothing was switched. {replaced.ErrorMessage}");

                imports[i].FlowId = replaced.Data.FlowId;
                imports[i].IsDone = true;
            }

            await transaction.CommitAsync(ct);
            await _broadcast.SendAsync(BroadcastTypeEnum.REPOSITORY_BRANCH_SWITCH, progress);

            // Written once the folder is let go. Each is already as the branch has it, so nothing changes.
            foreach (RepositoryBranchSwitchFlowDto imported in imports)
                _gitRepositoryService.Changed(imported.FlowId!.Value);

            return ResultDto<bool>.Success(true);
        }

        // The flows the switch touches, as the overlay lists them: the hidden ones, then the imports in
        // the order they go in, which are returned. New is new to the branch left: not here, or hidden.
        private static async Task<List<RepositoryBranchSwitchFlowDto>> ListAsync(AppDbContext dbContext, GitBranchSwitch branchSwitch, List<FlowScriptSchema> schemas, RepositoryBranchSwitchDto progress, CancellationToken ct)
        {
            List<Guid> publicIds = schemas.Select(x => x.Flow.PublicId).Concat(branchSwitch.GoneFlowIds).ToList();

            Dictionary<Guid, Flow> known = await dbContext.Flows
                .AsNoTracking()
                .Where(x => publicIds.Contains(x.PublicId))
                .Select(x => new Flow { Id = x.Id, PublicId = x.PublicId, Name = x.Name, IsSubFlow = x.IsSubFlow, IsHidden = x.IsHidden })
                .ToDictionaryAsync(x => x.PublicId, ct);

            foreach (Guid publicId in branchSwitch.GoneFlowIds)
            {
                if (known.TryGetValue(publicId, out Flow? flow))
                    progress.Flows.Add(new RepositoryBranchSwitchFlowDto { FlowId = flow.Id, Name = flow.Name, IsSubFlow = flow.IsSubFlow, Change = GitChangeKindEnum.DELETED });
            }

            List<RepositoryBranchSwitchFlowDto> imports = new List<RepositoryBranchSwitchFlowDto>();
            foreach (FlowScriptSchema schema in schemas)
            {
                known.TryGetValue(schema.Flow.PublicId, out Flow? flow);

                GitChangeKindEnum change = GitChangeKindEnum.MODIFIED;
                if (flow == null || flow.IsHidden)
                    change = GitChangeKindEnum.ADDED;

                imports.Add(new RepositoryBranchSwitchFlowDto
                {
                    FlowId = flow?.Id,
                    Name = schema.Flow.Name,
                    IsSubFlow = schema.Flow.IsSubFlow || flow?.IsSubFlow == true,
                    Change = change,
                });
            }

            progress.Flows.AddRange(imports);
            return imports;
        }

        // Sub-flows before the flows that call them, so each call finds its flow already there. Calls
        // that go round in a loop have no flow to start from, and what is left goes in as read.
        private static List<FlowScriptSchema> CalleesFirst(List<FlowScriptSchema> schemas)
        {
            List<FlowScriptSchema> ordered = new List<FlowScriptSchema>();
            List<FlowScriptSchema> pending = schemas.ToList();

            while (pending.Count > 0)
            {
                FlowScriptSchema? next = pending.FirstOrDefault(x => !pending.Any(y => y != x && Calls(x, y)));
                if (next == null)
                {
                    ordered.AddRange(pending);
                    break;
                }

                ordered.Add(next);
                pending.Remove(next);
            }

            return ordered;
        }

        private static bool Calls(FlowScriptSchema caller, FlowScriptSchema callee)
        {
            return caller.SubFlows.Values.Any(x => x.PublicId == callee.Flow.PublicId
                || (x.PublicId == null && string.Equals(x.Name, callee.Flow.Name, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
