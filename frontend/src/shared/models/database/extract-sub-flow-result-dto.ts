export interface ExtractSubFlowResultDto {
  subFlowId: number;

  /** The SUB_FLOW step left in its place, to select once the tree reloads. */
  flowStepId: number;

  movedCount: number;
}
