/**
 * Where a recording goes in a flow that already existed: child number index of a step, or of the
 * flow's top when parentFlowStepId is null. The editor's "Record steps here".
 */
export interface RecordingDraftPositionDto {
  flowId: number;
  parentFlowStepId: number | null;
  index: number;
}
