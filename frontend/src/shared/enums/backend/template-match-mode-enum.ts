export const TemplateMatchModeEnum = {
  SHAPE: "SHAPE",
  SHAPE_AND_BRIGHTNESS: "SHAPE_AND_BRIGHTNESS",
} as const;

export type TemplateMatchModeEnum =
  (typeof TemplateMatchModeEnum)[keyof typeof TemplateMatchModeEnum];
