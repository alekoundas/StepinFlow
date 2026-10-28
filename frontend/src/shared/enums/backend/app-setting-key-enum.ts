export const AppSettingKeyEnum = {
  RECORDING_HOTKEY_PAUSE: "RECORDING_HOTKEY_PAUSE",
  RECORDING_HOTKEY_STOP: "RECORDING_HOTKEY_STOP",
  RECORDING_HOTKEY_CHECK: "RECORDING_HOTKEY_CHECK",
  RECORDING_HOTKEY_SUBSTITUTE: "RECORDING_HOTKEY_SUBSTITUTE",

  HOTKEY_CONTINUE: "HOTKEY_CONTINUE",
  HOTKEY_STEP_INTO: "HOTKEY_STEP_INTO",
  HOTKEY_STEP_OVER: "HOTKEY_STEP_OVER",
  HOTKEY_PAUSE: "HOTKEY_PAUSE",
  HOTKEY_STOP: "HOTKEY_STOP",

  EXECUTION_SCREENSHOT_LIMIT: "EXECUTION_SCREENSHOT_LIMIT",

  AI_PROVIDER: "AI_PROVIDER",
  AI_MODEL: "AI_MODEL",
  AI_API_KEY: "AI_API_KEY",
  AI_SEND_SCREEN_CONTENT: "AI_SEND_SCREEN_CONTENT",
  AI_OLLAMA_CONTEXT_LENGTH: "AI_OLLAMA_CONTEXT_LENGTH",
  AI_OLLAMA_URL: "AI_OLLAMA_URL",

  GIT_AUTHOR_NAME: "GIT_AUTHOR_NAME",
  GIT_AUTHOR_EMAIL: "GIT_AUTHOR_EMAIL",
  GIT_REMOTE_TOKEN: "GIT_REMOTE_TOKEN",
} as const;

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

export type AppSettingKeyEnum =
  (typeof AppSettingKeyEnum)[keyof typeof AppSettingKeyEnum];
