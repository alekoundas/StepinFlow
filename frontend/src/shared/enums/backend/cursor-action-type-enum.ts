export const CursorActionTypeEnum = {
  CLICK: "CLICK",
  MOVE: "MOVE",
  SCROLL: "SCROLL",
  DRAG: "DRAG",
} as const;

export type CursorActionTypeEnum =
  (typeof CursorActionTypeEnum)[keyof typeof CursorActionTypeEnum];
