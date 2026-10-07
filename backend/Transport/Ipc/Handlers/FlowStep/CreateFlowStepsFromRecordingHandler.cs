using AutoMapper;
using Business.Flows.DataService;
using Core.Enums;
using Core.Helpers;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Transport.Ipc.Handlers
{
    /// <summary>
    /// Saves the steps a recording became, all at once, from the draft the wizard confirmed.
    ///
    /// The draft stops here. It is turned into linked rows - the shape the data service speaks -
    /// and the rows are saved in one go.
    /// </summary>
    public class CreateFlowStepsFromRecordingHandler
    {
        private readonly IMapper _mapper;
        private readonly DataService _dataService;

        public CreateFlowStepsFromRecordingHandler(IMapper mapper, DataService dataService)
        {
            _mapper = mapper;
            _dataService = dataService;
        }

        public async Task<ResultDto<FlowDraftResultDto>> HandleAsync(FlowDraftDto dto, CancellationToken ct)
        {
            if (dto.Steps.Count == 0)
                return ResultDto<FlowDraftResultDto>.Failure("There is nothing to save.");

            try
            {
                Dictionary<int, FlowStep> stepsByTempId = new Dictionary<int, FlowStep>();
                List<FlowStep> rows = ToLinkedRows(dto, stepsByTempId);

                ResultDto<int> created = await _dataService.FlowStep.CreateTreeAsync(dto.Target.TargetFlowId, dto.Target.TargetParentFlowStepId, dto.Target.TargetIndex, rows, ct);
                if (!created.IsSuccess)
                    return ResultDto<FlowDraftResultDto>.Failure(created.ErrorMessage ?? string.Empty);

                List<FlowStep> draftSteps = dto.Steps.Select(x => stepsByTempId[x.TempId]).ToList();
                FlowStep first = draftSteps.FirstOrDefault(x => x.ParentFlowStep == null) ?? draftSteps[0];

                return ResultDto<FlowDraftResultDto>.Success(new FlowDraftResultDto
                {
                    FlowId = created.Data,
                    FirstFlowStepId = first.Id,
                    CreatedCount = dto.Steps.Count,
                });
            }
            catch (Exception ex)
            {
                return ResultDto<FlowDraftResultDto>.Failure(ex.Message);
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        // Temp ids become links: the parent, the step a click reads, the points a recorded click
        // creates. A step under a branch hangs off that branch's row, made here the first time a
        // step needs it; the data service adds the branch rows nothing hangs under. A parent that
        // comes later in the draft is not one, and the step lands at the target.
        private List<FlowStep> ToLinkedRows(FlowDraftDto dto, Dictionary<int, FlowStep> stepsByTempId)
        {
            List<FlowStep> rows = new List<FlowStep>();
            Dictionary<(int TempId, FlowStepTypeEnum Branch), FlowStep> branches = new Dictionary<(int TempId, FlowStepTypeEnum Branch), FlowStep>();

            foreach (DraftStepDto draftStep in dto.Steps)
            {
                FlowStep step = _mapper.Map<FlowStep>(draftStep.Values);
                step.FlowStepTemplates = draftStep.Values.FlowStepTemplates.Select(x => _mapper.Map<FlowStepTemplate>(x)).ToList();

                if (draftStep.NewPoint != null)
                {
                    step.FlowPointId = null;
                    step.FlowPoint = NewPoint(draftStep.NewPoint);
                }

                if (draftStep.NewPointEnd != null)
                {
                    step.FlowPointEndId = null;
                    step.FlowPointEnd = NewPoint(draftStep.NewPointEnd);
                }

                if (draftStep.ParentTempId is int parentTempId && stepsByTempId.TryGetValue(parentTempId, out FlowStep? parent))
                    step.ParentFlowStep = ParentRow(parent, parentTempId, draftStep.ParentBranch, branches, rows);

                rows.Add(step);
                stepsByTempId[draftStep.TempId] = step;
            }

            foreach (DraftStepDto draftStep in dto.Steps.Where(x => x.ReferenceTempId != null))
            {
                if (!stepsByTempId.TryGetValue(draftStep.ReferenceTempId!.Value, out FlowStep? reference))
                    continue;

                FlowStep step = stepsByTempId[draftStep.TempId];
                step.FlowStepReferenceId = null;
                step.FlowStepReference = reference;
            }

            return rows;
        }

        // A branching parent owns nothing directly, so the step goes under the named branch's row.
        private static FlowStep ParentRow(
            FlowStep parent,
            int parentTempId,
            FlowStepTypeEnum? branchType,
            Dictionary<(int TempId, FlowStepTypeEnum Branch), FlowStep> branches,
            List<FlowStep> rows)
        {
            if (branchType == null)
                return parent;

            if (branches.TryGetValue((parentTempId, branchType.Value), out FlowStep? branch))
                return branch;

            branch = TreeStepHelper.CreateBranchChildren(parent).FirstOrDefault(x => x.FlowStepType == branchType.Value);
            if (branch == null)
                return parent;

            branches[(parentTempId, branchType.Value)] = branch;
            rows.Add(branch);

            return branch;
        }

        private static FlowPoint NewPoint(DraftPointDto dto)
        {
            return new FlowPoint
            {
                Name = dto.Name,
                LocationX = dto.LocationX,
                LocationY = dto.LocationY,
            };
        }
    }
}
