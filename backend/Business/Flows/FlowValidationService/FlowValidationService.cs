using Business.Flows.FlowValidationService.Rules;
using Core.Enums;
using Core.Helpers;
using Core.Models.Business;
using Core.Models.Database;
using Core.Models.Dtos;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Flows.FlowValidationService
{
    /// <summary>
    /// Per flow validate tree structre, flow step field validity and flow portability.
    /// </summary>
    public sealed class FlowValidationService : IFlowValidationService
    {
        // ================================================================
        // Public methods
        // ================================================================

        /// <summary>
        /// Load the whole flow tree and do the actual validation by calling "Validate()".
        /// </summary>
        public async Task<FlowValidationResultDto> ValidateAsync(AppDbContext dbContext, int flowId, CancellationToken ct)
        {
            LoadedFlows loaded = await LoadAsync(dbContext, [flowId], ct);
            Flow flow = loaded.Flows[flowId];

            return Validate(loaded.StepsByFlow[flowId].ToList(), loaded.TemplateCounts, flow.FlowAreas.ToList(), flow.FlowPoints.ToList(), FlowNames(flow));
        }

        /// <summary>
        /// Load the whole flow tree for all FlowIds and do the actual validation by calling "Validate()".
        /// </summary>
        public async Task<IReadOnlyDictionary<int, FlowValidationResultDto>> ValidateAsync(AppDbContext dbContext, IReadOnlyList<int> flowIds, CancellationToken ct)
        {
            LoadedFlows loaded = await LoadAsync(dbContext, flowIds, ct);
            Dictionary<int, FlowValidationResultDto> results = new Dictionary<int, FlowValidationResultDto>();

            foreach (Flow flow in loaded.Flows.Values)
                results[flow.Id] = Validate(loaded.StepsByFlow[flow.Id].ToList(), loaded.TemplateCounts, flow.FlowAreas.ToList(), flow.FlowPoints.ToList(), FlowNames(flow));

            return results;
        }

        /// <summary>
        /// Validate flow against tree structre, flow step field validity and flow portability
        /// </summary>
        public FlowValidationResultDto Validate(
            IReadOnlyList<FlowStep> steps,
            IReadOnlyDictionary<int, int> templateCountByStepId,
            IReadOnlyList<FlowArea> areas,
            IReadOnlyList<FlowPoint> points,
            IReadOnlyList<string> flowNames)
        {
            FlowValidationResultDto result = new FlowValidationResultDto();

            List<FlowStep> authored = steps
                .Where(x => !TreeStepHelper.IsBranchChild(x.FlowStepType))
                .ToList();

            // Validate step count.
            if (authored.Count == 0)
            {
                result.Add(null, string.Empty, ValidationSeverityEnum.ERROR, FlowValidationCodeEnum.FLOW_HAS_NO_STEPS, "This flow has no steps yet.");
                result.HasErrors = true;
                return result;
            }


            Dictionary<int, StepChainNode> byStepId = steps.ToDictionary(x => x.Id, x => new StepChainNode(x.Id, x.ParentFlowStepId, x.FlowStepType, x.Name, x.OrderNumber));
            ILookup<int?, FlowStep> childrenByParentId = steps.ToLookup(x => x.ParentFlowStepId);

            FlowStepValidator.Validate(authored, templateCountByStepId, result);
            FlowStructureValidator.Validate(authored, byStepId, childrenByParentId, flowNames, result);
            FlowPortabilityValidator.Validate(authored, areas, points, result);

            result.HasErrors = result.Issues.Any(x => x.Severity == ValidationSeverityEnum.ERROR);
            return result;
        }

     


        // ================================================================
        // Private methods
        // ================================================================

        // Load Flow with its relations.(areas, points and csvColumns).
        private static async Task<LoadedFlows> LoadAsync(AppDbContext dbContext, IReadOnlyList<int> flowIds, CancellationToken ct)
        {
            List<FlowStep> steps = await dbContext.FlowSteps
                .AsNoTracking()
                .Where(x => flowIds.Contains(x.RootId))
                .ToListAsync(ct);

            // Counted NOT Included! the templates are megabytes, only count matters here.
            Dictionary<int, int> templateCounts = await dbContext.FlowStepTemplates
                .AsNoTracking()
                .Where(x => flowIds.Contains(x.FlowStep.RootId))
                .GroupBy(x => x.FlowStepId)
                .Select(x => new { FlowStepId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.FlowStepId, x => x.Count, ct);

            // Load Flow with its relations.(areas, points and csvColumns).
            Dictionary<int, Flow> flows = await dbContext.Flows
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => flowIds.Contains(x.Id))
                .Select(x => new Flow
                {
                    Id = x.Id,
                    FlowAreas = x.FlowAreas.Select(area => new FlowArea
                    {
                        Id = area.Id,
                        Name = area.Name,
                        Type = area.Type,
                        ParentFlowAreaId = area.ParentFlowAreaId,
                        ProcessName = area.ProcessName,
                        TitlePattern = area.TitlePattern,
                        TabMatchValue = area.TabMatchValue,
                        MonitorDeviceName = area.MonitorDeviceName,
                    }).ToList(),
                    FlowPoints = x.FlowPoints.Select(point => new FlowPoint { Id = point.Id, Name = point.Name, FlowAreaId = point.FlowAreaId }).ToList(),
                    FlowCsvColumns = x.FlowCsvColumns.Select(column => new FlowCsvColumn { Name = column.Name }).ToList(),
                })
                .ToDictionaryAsync(x => x.Id, ct);

            foreach (int flowId in flowIds.Where(x => !flows.ContainsKey(x)))
                flows[flowId] = new Flow { Id = flowId };

            return new LoadedFlows(flows, steps.ToLookup(x => x.RootId), templateCounts);
        }

        // Areas, points and csv columns need to be unique between them.
        private static List<string> FlowNames(Flow flow)
        {
            return flow.FlowAreas.Select(x => x.Name)
                .Concat(flow.FlowPoints.Select(x => x.Name))
                .Concat(flow.FlowCsvColumns.Select(x => x.Name))
                .ToList();
        }



        // ================================================================
        // Private records
        // ================================================================

        private sealed record LoadedFlows(Dictionary<int, Flow> Flows, ILookup<int, FlowStep> StepsByFlow, Dictionary<int, int> TemplateCounts);
    }
}
