export const AiChatEventTypeEnum = {
  STARTED: "STARTED",
  TOOL_STARTED: "TOOL_STARTED",
  TOOL_FINISHED: "TOOL_FINISHED",
  TEXT: "TEXT",
  DONE: "DONE",
  STOPPED: "STOPPED",
  FAILED: "FAILED",
} as const;

export type AiChatEventTypeEnum =
  (typeof AiChatEventTypeEnum)[keyof typeof AiChatEventTypeEnum];
