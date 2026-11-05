export const AiLocalModelStateEnum = {
  NOT_ANSWERING: "NOT_ANSWERING",
  NOT_CHOSEN: "NOT_CHOSEN",
  NOT_DOWNLOADED: "NOT_DOWNLOADED",
  NOT_IN_MEMORY: "NOT_IN_MEMORY",
  IN_MEMORY: "IN_MEMORY",
} as const;

export type AiLocalModelStateEnum =
  (typeof AiLocalModelStateEnum)[keyof typeof AiLocalModelStateEnum];
