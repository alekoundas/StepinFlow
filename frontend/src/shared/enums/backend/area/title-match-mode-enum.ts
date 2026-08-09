export const TitleMatchModeEnum = {
  CONTAINS: "CONTAINS",
  EQUALS: "EQUALS",
  STARTS_WITH: "STARTS_WITH",
  REGEX: "REGEX",
} as const;

export type TitleMatchModeEnum =
  (typeof TitleMatchModeEnum)[keyof typeof TitleMatchModeEnum];
