// The destination is "child number targetIndex of this parent". Exactly one of the two parents
// is set: a step lands either under another step or at the root of the Flow.
export interface FlowStepMoveDto {
  flowStepId: number;
  targetParentFlowStepId?: number;
  targetFlowId?: number;
  targetIndex: number;
}
