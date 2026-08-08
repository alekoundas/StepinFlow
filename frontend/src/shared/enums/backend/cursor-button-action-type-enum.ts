export const cursorButtonActionTypeEnum = {
  SINGLE_CLICK: "SINGLE_CLICK",
  DOUBLE_CLICK: "DOUBLE_CLICK",
  HOLD_CLICK: "HOLD_CLICK",
  RELEASE_CLICK: "RELEASE_CLICK",
} as const;

export type CursorButtonActionTypeEnum =
  (typeof cursorButtonActionTypeEnum)[keyof typeof cursorButtonActionTypeEnum];
