export const TemplateMatchModeEnum = {
  SHAPE: "SHAPE",
  SHAPE_AND_COLOUR: "SHAPE_AND_COLOUR",
} as const;

export type TemplateMatchModeEnum =
  (typeof TemplateMatchModeEnum)[keyof typeof TemplateMatchModeEnum];
