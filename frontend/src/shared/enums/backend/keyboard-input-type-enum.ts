export const KeyboardInputTypeEnum = {
  TEXT: "TEXT",
  COMBINATION: "COMBINATION",
} as const;

export type KeyboardInputTypeEnum =
  (typeof KeyboardInputTypeEnum)[keyof typeof KeyboardInputTypeEnum];
