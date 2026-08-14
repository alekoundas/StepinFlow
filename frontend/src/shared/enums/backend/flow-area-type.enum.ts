export const FlowAreaTypeEnum = {
  CUSTOM: "CUSTOM",
  APPLICATION: "APPLICATION",
  BROWSER_TAB: "BROWSER_TAB",
  MONITOR: "MONITOR",
} as const;

export type FlowAreaTypeEnum =
  (typeof FlowAreaTypeEnum)[keyof typeof FlowAreaTypeEnum];
