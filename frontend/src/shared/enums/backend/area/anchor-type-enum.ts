export const AnchorTypeEnum = {
  TOP_LEFT: "TOP_LEFT",
  TOP_RIGHT: "TOP_RIGHT",
  BOTTOM_LEFT: "BOTTOM_LEFT",
  BOTTOM_RIGHT: "BOTTOM_RIGHT",
  CENTER: "CENTER",
} as const;

export type AnchorTypeEnum =
  (typeof AnchorTypeEnum)[keyof typeof AnchorTypeEnum];
