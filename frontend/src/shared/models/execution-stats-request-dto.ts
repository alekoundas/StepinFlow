export interface ExecutionStatsRequestDto {
  /** One flow's executions only. Null for every flow's. */
  flowId: number | null;

  /** Only the executions started in the last this many days. Null for all of them. */
  days: number | null;
}
