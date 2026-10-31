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
 * from this and the execution page blocks Start from the same list - the list the backend refuses
 * Start on - so they cannot end up disagreeing about whether a flow is runnable.
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
  noteCount: number;
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
      noteCount: 0,
      messages: [],
    };

    if (issue.severity === ValidationSeverityEnum.ERROR) current.errorCount += 1;
    else if (issue.severity === ValidationSeverityEnum.WARNING) current.warningCount += 1;
    else current.noteCount += 1;

    current.messages.push(issue.message);
    byStep.set(issue.flowStepId, current);
  }

  return byStep;
};

const plural = (count: number, noun: string) => `${count} ${noun}${count === 1 ? "" : "s"}`;

export interface IssuesBelow {
  errorCount: number;
  warningCount: number;
}

/** The errors and warnings under each step, so a closed row can say there is something inside it. */
export const groupIssuesBelowStep = (
  issues: FlowValidationIssueDto[],
): Map<number, IssuesBelow> => {
  const below = new Map<number, IssuesBelow>();

  for (const issue of issues) {
    if (issue.severity === ValidationSeverityEnum.INFO) continue;

    for (const id of issue.parentFlowStepIds) {
      const current = below.get(id) ?? { errorCount: 0, warningCount: 0 };

      if (issue.severity === ValidationSeverityEnum.ERROR) current.errorCount += 1;
      else current.warningCount += 1;

      below.set(id, current);
    }
  }

  return below;
};

export const issuesBelowText = (below: IssuesBelow): string => {
  const parts: string[] = [];
  if (below.errorCount > 0) parts.push(plural(below.errorCount, "error"));
  if (below.warningCount > 0) parts.push(plural(below.warningCount, "warning"));

  return `${parts.join(" and ")} inside`;
};

/** Errors first, then warnings, then notes. */
export const severityRank = (severity: ValidationSeverityEnum): number => {
  if (severity === ValidationSeverityEnum.ERROR) return 0;
  if (severity === ValidationSeverityEnum.WARNING) return 1;
  return 2;
};

export const severityLookOf = (severity: ValidationSeverityEnum): { color: string; icon: string } => {
  if (severity === ValidationSeverityEnum.ERROR) return { color: "var(--red-400)", icon: "exclamation-triangle" };
  if (severity === ValidationSeverityEnum.WARNING) return { color: "var(--yellow-500)", icon: "exclamation-triangle" };
  return { color: "var(--text-color-secondary)", icon: "info-circle" };
};

/** What the flow's issues add up to. The worst kind decides the colour and is the one counted. */
export const flowHealthOf = (issues: FlowValidationIssueDto[]): { color: string; text: string } => {
  const errors = issues.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length;
  const warnings = issues.filter((x) => x.severity === ValidationSeverityEnum.WARNING).length;
  const notes = issues.filter((x) => x.severity === ValidationSeverityEnum.INFO).length;

  if (errors > 0) return { color: severityLookOf(ValidationSeverityEnum.ERROR).color, text: plural(errors, "error") };
  if (warnings > 0) return { color: severityLookOf(ValidationSeverityEnum.WARNING).color, text: plural(warnings, "warning") };
  if (notes > 0) return { color: severityLookOf(ValidationSeverityEnum.INFO).color, text: plural(notes, "note") };
  return { color: "var(--green-400)", text: "No issues" };
};
