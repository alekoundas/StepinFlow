export const AiProviderEnum = {
  NONE: "NONE",
  OPENAI: "OPENAI",
  OLLAMA: "OLLAMA",
} as const;

export type AiProviderEnum =
  (typeof AiProviderEnum)[keyof typeof AiProviderEnum];
