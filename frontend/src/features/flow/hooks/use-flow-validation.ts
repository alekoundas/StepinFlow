import { useQuery } from "@tanstack/react-query";
import { backendApiService } from "@/shared/services/backend-api-service";
import {
  ValidationSeverityEnum,
  type FlowValidationIssueDto,
} from "@/shared/models/database/flow-validation-result-dto";

export const flowValidationKeys = {
  all: ["flowValidation"] as const,
  detail: (flowId: number) => ["flowValidation", flowId] as const,
} as const;

/**
 * One answer for the whole flow, shared by everything that shows it. The tree badges its rows
 * from this and the execution page will block Run from the same list, so the two cannot end up
 * disagreeing about whether a flow is runnable.
 */
export function useFlowValidation(flowId: number | null) {
  return useQuery({
    queryKey: flowId ? flowValidationKeys.detail(flowId) : ["flowValidation", "disabled"],
    queryFn: () => backendApiService.Flow.validate(flowId!),
    enabled: !!flowId,
  });
}

export interface StepIssues {
  errorCount: number;
  warningCount: number;
  messages: string[];
}

/** Issues grouped by the step they belong to, ready for a tree row to read by entity id. */
export const groupIssuesByStep = (
  issues: FlowValidationIssueDto[],
): Map<number, StepIssues> => {
  const byStep = new Map<number, StepIssues>();

  for (const issue of issues) {
    if (issue.flowStepId == null) continue;

    const current = byStep.get(issue.flowStepId) ?? {
      errorCount: 0,
      warningCount: 0,
      messages: [],
    };

    if (issue.severity === ValidationSeverityEnum.ERROR) current.errorCount += 1;
    else current.warningCount += 1;

    current.messages.push(issue.message);
    byStep.set(issue.flowStepId, current);
  }

  return byStep;
};
