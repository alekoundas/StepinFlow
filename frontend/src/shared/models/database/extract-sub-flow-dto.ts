export interface ExtractSubFlowDto {
  flowStepId: number;
  name: string;

  // Where the extracted step was, which is where the SUB_FLOW step goes.
  sourceRootId: number;
  sourceFlowId?: number | null;
  sourceParentFlowStepId?: number | null;
  sourceOrderNumber: number;
}
