export const SystemActionTypeEnum = {
  LOCK_WORKSTATION: "LOCK_WORKSTATION",
  SLEEP_PC: "SLEEP_PC",
  MONITOR_OFF: "MONITOR_OFF",
  MONITOR_ON: "MONITOR_ON",
} as const;

export type SystemActionTypeEnum =
  (typeof SystemActionTypeEnum)[keyof typeof SystemActionTypeEnum];
