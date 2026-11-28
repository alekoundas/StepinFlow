export const CursorButtonActionTypeEnum = {
  SINGLE_CLICK: "SINGLE_CLICK",
  DOUBLE_CLICK: "DOUBLE_CLICK",
  HOLD_CLICK: "HOLD_CLICK",
  RELEASE_CLICK: "RELEASE_CLICK",
} as const;

export type CursorButtonActionTypeEnum =
  (typeof CursorButtonActionTypeEnum)[keyof typeof CursorButtonActionTypeEnum];
