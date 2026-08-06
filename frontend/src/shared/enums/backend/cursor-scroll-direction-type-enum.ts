export const CursorScrollDirectionTypeEnum = {
  UP: "UP",
  DOWN: "DOWN",
  LEFT: "LEFT",
  RIGHT: "RIGHT",
} as const;

export type CursorScrollDirectionTypeEnum =
  (typeof CursorScrollDirectionTypeEnum)[keyof typeof CursorScrollDirectionTypeEnum];
