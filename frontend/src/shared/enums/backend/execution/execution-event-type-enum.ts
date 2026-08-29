export const ExecutionEventTypeEnum = {
  STEP_STARTED: "STEP_STARTED",
  STEP_FINISHED: "STEP_FINISHED",
  PAUSED: "PAUSED",
  RUN_ENDED: "RUN_ENDED",
} as const;

export type ExecutionEventTypeEnum =
  (typeof ExecutionEventTypeEnum)[keyof typeof ExecutionEventTypeEnum];
