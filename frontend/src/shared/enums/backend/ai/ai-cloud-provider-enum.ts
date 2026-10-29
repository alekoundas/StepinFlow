export const AiCloudProviderEnum = {
  NONE: "NONE",
  OPENAI: "OPENAI",
} as const;

export type AiCloudProviderEnum =
  (typeof AiCloudProviderEnum)[keyof typeof AiCloudProviderEnum];
