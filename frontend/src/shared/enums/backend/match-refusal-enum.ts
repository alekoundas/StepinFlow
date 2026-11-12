export const MatchRefusalEnum = {
  COLOUR: "COLOUR",
  CONTRAST: "CONTRAST",
} as const;

export type MatchRefusalEnum = (typeof MatchRefusalEnum)[keyof typeof MatchRefusalEnum];
