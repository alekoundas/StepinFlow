export const CursorActionTypeEnum = {
  SINGLE_CLICK: "SINGLE_CLICK",
  DOUBLE_CLICK: "DOUBLE_CLICK",
  HOLD_CLICK: "HOLD_CLICK",
  RELEASE_CLICK: "RELEASE_CLICK",
} as const;

export type CursorActionTypeEnum =
  (typeof CursorActionTypeEnum)[keyof typeof CursorActionTypeEnum];
