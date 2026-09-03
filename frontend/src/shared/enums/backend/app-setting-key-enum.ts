export const AppSettingKeyEnum = {
  RECORDING_CAPTURE_WIDTH: "RECORDING_CAPTURE_WIDTH",
  RECORDING_CAPTURE_HEIGHT: "RECORDING_CAPTURE_HEIGHT",

  HOTKEY_CONTINUE: "HOTKEY_CONTINUE",
  HOTKEY_STEP_INTO: "HOTKEY_STEP_INTO",
  HOTKEY_STEP_OVER: "HOTKEY_STEP_OVER",
  HOTKEY_PAUSE: "HOTKEY_PAUSE",
  HOTKEY_STOP: "HOTKEY_STOP",

  EXECUTION_SCREENSHOT_RING_SIZE: "EXECUTION_SCREENSHOT_RING_SIZE",
  EXECUTION_SCREENSHOT_LIMIT: "EXECUTION_SCREENSHOT_LIMIT",

  AI_PROVIDER: "AI_PROVIDER",
  AI_MODEL: "AI_MODEL",
  AI_API_KEY: "AI_API_KEY",
  AI_SEND_SCREEN_CONTENT: "AI_SEND_SCREEN_CONTENT",
  AI_OLLAMA_URL: "AI_OLLAMA_URL",
} as const;

/** What control a setting needs. The page renders on this rather than branching on the key. */
export const AppSettingKindEnum = {
  INT: "INT",
  HOTKEY: "HOTKEY",
  TEXT: "TEXT",
  SECRET: "SECRET",
  CHOICE: "CHOICE",
  BOOL: "BOOL",
} as const;

export type AppSettingKindEnum =
  (typeof AppSettingKindEnum)[keyof typeof AppSettingKindEnum];

export type AppSettingKeyEnum =
  (typeof AppSettingKeyEnum)[keyof typeof AppSettingKeyEnum];
