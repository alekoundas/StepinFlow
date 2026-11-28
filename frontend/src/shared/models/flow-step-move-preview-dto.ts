import type { FlowStepBrokenReferenceDto } from "@/shared/models/flow-step-broken-reference-dto";

export interface FlowStepMovePreviewDto {
  isValid: boolean;
  errorMessage?: string;

  movedStepName: string;
  targetParentName: string;
  movedDescendantCount: number;
  isReorderOnly: boolean;

  brokenReferences: FlowStepBrokenReferenceDto[];
}
