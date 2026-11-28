import type { ValidationSeverityEnum } from "@/shared/enums/backend/validation-severity-enum";

export interface FlowValidationIssueDto {
  // Null for a problem with the flow itself rather than one of its steps.
  flowStepId?: number | null;
  flowStepName: string;

  // The steps above it, from the top down, and their names. Empty for a step at the top of the flow.
  parentFlowStepIds: number[];
  path: string[];

  severity: ValidationSeverityEnum;
  code: string;
  message: string;
}
