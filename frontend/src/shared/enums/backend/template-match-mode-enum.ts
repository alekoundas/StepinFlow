export const TemplateMatchModeEnum = {
  SqDiff: "SqDiff",
  SqDiffNormed: "SqDiffNormed",
  CCorr: "CCorr",
  CCorrNormed: "CCorrNormed",
  CCoeff: "CCoeff",
  CCoeffNormed: "CCoeffNormed",
} as const;

export type TemplateMatchModeEnum =
  (typeof TemplateMatchModeEnum)[keyof typeof TemplateMatchModeEnum];
