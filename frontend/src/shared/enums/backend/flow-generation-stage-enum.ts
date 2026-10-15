// What a generation is working on, in order. Reading an answer back is part of asking.
export const FlowGenerationStageEnum = {
  NONE: "NONE", // Not generating
  WRITING_DRAFT: "WRITING_DRAFT", // The plain draft, as a script
  ASKING_MODEL: "ASKING_MODEL", // The model shaping it, one attempt after another
  SAVING: "SAVING", // Imported into the flow, and validated
} as const;

export type FlowGenerationStageEnum =
  (typeof FlowGenerationStageEnum)[keyof typeof FlowGenerationStageEnum];
