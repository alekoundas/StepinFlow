using Business.Services.FlowValidationService;
using Core.Enums;
using Core.Models.Database;
using Core.Models.Dtos;

namespace Business.Services.RecordingService
{
    /// <summary>
    /// Fills in what each proposed step is still missing.
    ///
    /// Runs the real FlowValidator rather than a second set of rules: the wizard's list of gaps
    /// and the tree's warning badges then cannot disagree about whether a step is finished. The
    /// draft is materialised as FlowStep objects with negative ids, which nothing persists and
    /// which keep the parent chain walkable while the real ids do not exist yet.
    /// </summary>
    public static class DraftValidator
    {
        public static void Annotate(FlowDraftDto draft, IFlowValidator validator)
        {
            if (draft.Steps.Count == 0)
                return;

            Dictionary<int, FlowStep> byTempId = new Dictionary<int, FlowStep>();
            List<FlowStep> steps = new List<FlowStep>();

            foreach (DraftStepDto draftStep in draft.Steps)
            {
                FlowStep step = Materialise(draftStep);
                byTempId[draftStep.TempId] = step;
                steps.Add(step);
            }

            // Second pass: parents only resolve once every step has an id.
            foreach (DraftStepDto draftStep in draft.Steps)
            {
                if (draftStep.ParentTempId is int parentTempId &&
                    byTempId.TryGetValue(parentTempId, out FlowStep? parent))
                    byTempId[draftStep.TempId].ParentFlowStepId = parent.Id;
            }

            // The branches a save would create, so a branching step is not reported as childless
            // for a reason the user cannot act on.
            int nextId = -(draft.Steps.Count + 1);
            foreach (FlowStep step in steps.ToList())
            {
                foreach (FlowStep branch in TreeStepHelperBranches(step))
                {
                    branch.Id = nextId--;
                    branch.ParentFlowStepId = step.Id;
                    steps.Add(branch);
                }
            }

            Dictionary<int, int> templateCounts = draft.Steps.ToDictionary(
                x => byTempId[x.TempId].Id,
                x => x.Values.FlowStepImages.Count());

            FlowValidationResultDto result = validator.Validate(steps, templateCounts);

            Dictionary<int, int> tempIdByStepId = byTempId.ToDictionary(x => x.Value.Id, x => x.Key);
            Dictionary<int, DraftStepDto> draftByTempId = draft.Steps.ToDictionary(x => x.TempId);

            foreach (DraftStepDto draftStep in draft.Steps)
                draftStep.Unresolved.Clear();

            foreach (FlowValidationIssueDto issue in result.Issues)
            {
                if (issue.FlowStepId is not int stepId ||
                    !tempIdByStepId.TryGetValue(stepId, out int tempId) ||
                    !draftByTempId.TryGetValue(tempId, out DraftStepDto? draftStep))
                    continue;

                draftStep.Unresolved.Add(new DraftIssueDto
                {
                    Code = issue.Code,
                    Severity = issue.Severity,
                    Message = issue.Message,
                });
            }
        }


        // ================================================================
        // Private methods
        // ================================================================

        private static FlowStep Materialise(DraftStepDto draftStep)
        {
            FlowStepDto values = draftStep.Values;

            return new FlowStep
            {
                // Negative so it can never collide with a saved row if one ever leaks in here.
                Id = -draftStep.TempId,
                Name = values.Name,
                FlowStepType = values.FlowStepType,
                OrderNumber = values.OrderNumber,

                WaitForMilliseconds = values.WaitForMilliseconds,
                LoopCount = values.LoopCount,
                IsLoopInfinite = values.IsLoopInfinite,

                SearchMode = values.SearchMode,
                MaxMatches = values.MaxMatches,
                PollIntervalMilliseconds = values.PollIntervalMilliseconds,
                TimeoutMilliseconds = values.TimeoutMilliseconds,

                RunCommandShell = values.RunCommandShell,
                RunCommandPreset = values.RunCommandPreset,
                RunCommandPresetValue = values.RunCommandPresetValue,
                RunCommand = values.RunCommand,

                OcrLanguage = values.OcrLanguage,
                ConditionText = values.ConditionText,
                ConditionTextEnd = values.ConditionTextEnd,
                ConditionType = values.ConditionType,

                WindowWidth = values.WindowWidth,
                WindowHeight = values.WindowHeight,

                KeyboardInputText = values.KeyboardInputText,
                KeyboardInputType = values.KeyboardInputType,

                // A point the save is going to create counts as present: the user has nothing to
                // fix, so reporting it would be a gap they cannot close.
                IsPointCustom = values.IsPointCustom,
                IsPointEndCustom = values.IsPointEndCustom,
                FlowPointId = draftStep.NewPoint != null ? -draftStep.TempId : values.FlowPointId,
                FlowPointEndId = draftStep.NewPointEnd != null ? -draftStep.TempId : values.FlowPointEndId,
                FlowStepReferenceId = values.FlowStepReferenceId,
                FlowStepReferenceEndId = values.FlowStepReferenceEndId,
                FlowAreaId = values.FlowAreaId,
                SubFlowId = values.SubFlowId,

                CursorButtonType = values.CursorButtonType,
                CursorButtonActionType = values.CursorButtonActionType,
                CursorScrollDirectionType = values.CursorScrollDirectionType,
            };
        }

        private static IReadOnlyList<FlowStep> TreeStepHelperBranches(FlowStep step) =>
            Core.Helpers.TreeStepHelper.CreateBranchChildren(step);
    }
}
