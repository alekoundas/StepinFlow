export const ExecutionStatusEnum = {
  RUNNING: "RUNNING",
  COMPLETED: "COMPLETED",
  FAILED: "FAILED",
  INCONCLUSIVE: "INCONCLUSIVE",
  STOPPED: "STOPPED",
  ERRORED: "ERRORED",
  ABANDONED: "ABANDONED",
} as const;

export type ExecutionStatusEnum =
  (typeof ExecutionStatusEnum)[keyof typeof ExecutionStatusEnum];
