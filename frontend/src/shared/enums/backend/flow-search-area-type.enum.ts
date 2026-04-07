export const FlowSearchAreaTypeEnum = {
  CUSTOM: "CUSTOM",
  APPLICATION: "APPLICATION",
  MONITOR: "MONITOR",
} as const;

export type FlowSearchAreaTypeEnum =
  (typeof FlowSearchAreaTypeEnum)[keyof typeof FlowSearchAreaTypeEnum];
