export const ExecutionHistoryLevelEnum = {
  NONE: "NONE",
  STEPS: "STEPS",
  STEPS_AND_IMAGES: "STEPS_AND_IMAGES",
} as const;

export type ExecutionHistoryLevelEnum =
  (typeof ExecutionHistoryLevelEnum)[keyof typeof ExecutionHistoryLevelEnum];
