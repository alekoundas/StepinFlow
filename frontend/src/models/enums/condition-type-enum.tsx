export const ConditionTypeEnum = {
  IN: "IN",
  EQUALS: "EQUALS",
  NOT_EQUALS: "NOT_EQUALS",
  NONE: "NONE",
} as const;

export type ConditionTypeEnum =
  (typeof ConditionTypeEnum)[keyof typeof ConditionTypeEnum];
