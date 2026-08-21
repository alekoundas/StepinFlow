export interface ExtractSubFlowDto {
  flowStepId: number;
  name: string;

  // Where the extracted step was, which is where the SUB_FLOW step goes.
  sourceRootId: number;
  sourceFlowId?: number | null;
  sourceParentFlowStepId?: number | null;
  sourceOrderNumber: number;
}

export interface ExtractSubFlowResultDto {
  subFlowId: number;

  /** The SUB_FLOW step left in its place, to select once the tree reloads. */
  flowStepId: number;

  movedCount: number;
}
