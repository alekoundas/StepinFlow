export interface ExecutionStepRangeDto {
  executionId: number;

  /** Both ends are included. */
  firstSequence: number;
  lastSequence: number;
}
