export const ValidationSeverityEnum = {
  ERROR: "ERROR",
  WARNING: "WARNING",
  INFO: "INFO", // Runs as meant, but worth knowing, quieter than a warning
} as const;

export type ValidationSeverityEnum =
  (typeof ValidationSeverityEnum)[keyof typeof ValidationSeverityEnum];

export interface FlowValidationIssueDto {
  // Null for a problem with the flow itself rather than one of its steps.
  flowStepId?: number | null;
  flowStepName: string;

  severity: ValidationSeverityEnum;
  code: string;
  message: string;
}

export interface FlowValidationResultDto {
  hasErrors: boolean;
  issues: FlowValidationIssueDto[];
}
