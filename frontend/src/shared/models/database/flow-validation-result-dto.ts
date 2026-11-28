import type { FlowValidationIssueDto } from "@/shared/models/database/flow-validation-issue-dto";

export interface FlowValidationResultDto {
  hasErrors: boolean;
  issues: FlowValidationIssueDto[];
}
