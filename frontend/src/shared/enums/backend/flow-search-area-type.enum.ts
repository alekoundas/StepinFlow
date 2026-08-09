export const FlowSearchAreaTypeEnum = {
  CUSTOM: "CUSTOM",
  APPLICATION: "APPLICATION",
  BROWSER_TAB: "BROWSER_TAB",
  MONITOR: "MONITOR",
} as const;

export type FlowSearchAreaTypeEnum =
  (typeof FlowSearchAreaTypeEnum)[keyof typeof FlowSearchAreaTypeEnum];
