export const RunStateEnum = {
  RUNNING: "RUNNING",
  PAUSED: "PAUSED",
  STOPPING: "STOPPING",
  FINISHED: "FINISHED",
} as const;

export type RunStateEnum = (typeof RunStateEnum)[keyof typeof RunStateEnum];
