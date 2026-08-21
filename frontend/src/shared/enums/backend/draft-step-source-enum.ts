export const DraftStepSourceEnum = {
  RECORDING: "RECORDING",
  AI: "AI",
} as const;

export type DraftStepSourceEnum =
  (typeof DraftStepSourceEnum)[keyof typeof DraftStepSourceEnum];
