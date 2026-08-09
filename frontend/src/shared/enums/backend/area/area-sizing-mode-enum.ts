export const AreaSizingModeEnum = {
  ABSOLUTE_PX: "ABSOLUTE_PX",
  RATIO: "RATIO",
} as const;

export type AreaSizingModeEnum =
  (typeof AreaSizingModeEnum)[keyof typeof AreaSizingModeEnum];
