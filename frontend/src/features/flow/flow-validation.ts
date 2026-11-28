import { ValidationSeverityEnum } from "@/shared/enums/backend/validation-severity-enum";
import type { FlowValidationIssueDto } from "@/shared/models/database/flow-validation-issue-dto";

export interface StepIssues {
  errorCount: number;
  warningCount: number;
  noteCount: number;
  messages: string[];
}

/** Issues grouped by the step they belong to, ready for a tree row to read by entity id. */
const groupIssuesByStep = (
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
const groupIssuesBelowStep = (
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

const issuesBelowText = (below: IssuesBelow): string => {
  const parts: string[] = [];
  if (below.errorCount > 0) parts.push(plural(below.errorCount, "error"));
  if (below.warningCount > 0) parts.push(plural(below.warningCount, "warning"));

  return `${parts.join(" and ")} inside`;
};

/** Errors first, then warnings, then notes. */
const severityRank = (severity: ValidationSeverityEnum): number => {
  if (severity === ValidationSeverityEnum.ERROR) return 0;
  if (severity === ValidationSeverityEnum.WARNING) return 1;
  return 2;
};

const severityLookOf = (severity: ValidationSeverityEnum): { color: string; icon: string } => {
  if (severity === ValidationSeverityEnum.ERROR) return { color: "var(--red-400)", icon: "exclamation-triangle" };
  if (severity === ValidationSeverityEnum.WARNING) return { color: "var(--yellow-500)", icon: "exclamation-triangle" };
  return { color: "var(--text-color-secondary)", icon: "info-circle" };
};

/** What the flow's issues add up to. The worst kind decides the colour and is the one counted. */
const flowHealthOf = (issues: FlowValidationIssueDto[]): { color: string; text: string } => {
  const errors = issues.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length;
  const warnings = issues.filter((x) => x.severity === ValidationSeverityEnum.WARNING).length;
  const notes = issues.filter((x) => x.severity === ValidationSeverityEnum.INFO).length;

  if (errors > 0) return { color: severityLookOf(ValidationSeverityEnum.ERROR).color, text: plural(errors, "error") };
  if (warnings > 0) return { color: severityLookOf(ValidationSeverityEnum.WARNING).color, text: plural(warnings, "warning") };
  if (notes > 0) return { color: severityLookOf(ValidationSeverityEnum.INFO).color, text: plural(notes, "note") };
  return { color: "var(--green-400)", text: "No issues" };
};

export const FlowValidation = {
  groupIssuesByStep,
  groupIssuesBelowStep,
  issuesBelowText,
  severityRank,
  severityLookOf,
  flowHealthOf,
};
