/** How a set of executions ended, counted by outcome, for the executions page's tiles and filters. */
export interface ExecutionStatsDto {
  total: number;
  executing: number;
  completed: number;
  failed: number;
  errored: number;
  inconclusive: number;
  stopped: number;
  abandoned: number;

  /** When the newest failed or errored execution started. Null when none did. */
  lastFailureOn?: string | null;

  /** The median time a completed execution took. Null when none completed. */
  usualDurationMilliseconds?: number | null;
}
