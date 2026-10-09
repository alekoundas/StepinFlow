// What a key combination step does with its keys. Hold and Release are two halves of one gesture:
// whatever runs between them happens with the keys down.
export const KeyboardKeyActionTypeEnum = {
  PRESS: "PRESS",
  HOLD: "HOLD",
  RELEASE: "RELEASE",
} as const;

export type KeyboardKeyActionTypeEnum =
  (typeof KeyboardKeyActionTypeEnum)[keyof typeof KeyboardKeyActionTypeEnum];
