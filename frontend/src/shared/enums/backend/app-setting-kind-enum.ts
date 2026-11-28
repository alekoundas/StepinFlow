/** What control a setting needs. The page renders on this rather than branching on the key. */
export const AppSettingKindEnum = {
  INT: "INT",
  HOTKEY: "HOTKEY",
  GESTURE: "GESTURE", // Modifiers and a mouse button, pressed while recording
  TEXT: "TEXT",
  SECRET: "SECRET",
  CHOICE: "CHOICE",
  BOOL: "BOOL",
} as const;

export type AppSettingKindEnum =
  (typeof AppSettingKindEnum)[keyof typeof AppSettingKindEnum];
