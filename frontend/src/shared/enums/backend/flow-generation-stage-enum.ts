// What a generation is working on, in order. Reading an answer back is part of asking.
export const FlowGenerationStageEnum = {
  NONE: "NONE", // Not generating
  LOOKING_AT_SCREENS: "LOOKING_AT_SCREENS", // The local model naming wordless clicks and describing new pages
  WRITING_DRAFT: "WRITING_DRAFT", // The plain draft, as a script
  WAITING_TO_SEND: "WAITING_TO_SEND", // A cloud model's request, with text read off the screen in it, shown until the tester sends it
  ASKING_MODEL: "ASKING_MODEL", // The model shaping it, one attempt after another
  SAVING: "SAVING", // Imported into the flow, and validated
} as const;

export type FlowGenerationStageEnum =
  (typeof FlowGenerationStageEnum)[keyof typeof FlowGenerationStageEnum];
