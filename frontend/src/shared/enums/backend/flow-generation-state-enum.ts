// Where the model's draft of a reviewed recording is.
export const FlowGenerationStateEnum = {
  NONE: "NONE", // Nothing asked for, or cancelled
  GENERATING: "GENERATING", // Working; the stage says on what
  DONE: "DONE", // Saved, as the model's draft or the plain one; the result says which
} as const;

export type FlowGenerationStateEnum =
  (typeof FlowGenerationStateEnum)[keyof typeof FlowGenerationStateEnum];
