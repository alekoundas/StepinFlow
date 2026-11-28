// A copy of flowStepId and everything under it, placed the way a move places a step.
export interface FlowStepPasteDto {
  flowStepId: number;
  targetParentFlowStepId?: number;
  targetFlowId?: number;
  targetIndex: number;
}
