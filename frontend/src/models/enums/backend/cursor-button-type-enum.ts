export const CursorButtonTypeEnum = {
  LEFT_BUTTON: "LEFT_BUTTON",
  RIGHT_BUTTON: "RIGHT_BUTTON",
} as const;

export type CursorButtonTypeEnum =
  (typeof CursorButtonTypeEnum)[keyof typeof CursorButtonTypeEnum];
