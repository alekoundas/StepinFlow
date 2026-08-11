export const ConditionTypeEnum = {
  // Text
  EQUALS: "EQUALS",
  NOT_EQUALS: "NOT_EQUALS",
  CONTAINS: "CONTAINS",
  NOT_CONTAINS: "NOT_CONTAINS",
  MATCHES_REGEX: "MATCHES_REGEX",
  IS_EMPTY: "IS_EMPTY",
  IS_NOT_EMPTY: "IS_NOT_EMPTY",

  // Numeric
  GREATER_THAN: "GREATER_THAN",
  LESS_THAN: "LESS_THAN",
  BETWEEN: "BETWEEN",
} as const;

export type ConditionTypeEnum =
  (typeof ConditionTypeEnum)[keyof typeof ConditionTypeEnum];
