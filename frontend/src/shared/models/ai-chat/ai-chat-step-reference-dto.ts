export interface AiChatStepReferenceDto {
  flowStepId: number;

  /** The flow it belongs to. A sub-flow's step opens the sub-flow. */
  flowId: number;

  /** As the model read it, which is what its answer calls it. */
  name: string;
}
