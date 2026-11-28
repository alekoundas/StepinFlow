export interface FlowStepBrokenReferenceDto {
  flowStepId: number;
  flowStepName: string;
  referencedStepName: string;
  isEndReference: boolean;
}
