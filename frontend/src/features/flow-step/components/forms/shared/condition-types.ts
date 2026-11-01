import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";

export const CONDITION_LABELS: Record<ConditionTypeEnum, string> = {
  [ConditionTypeEnum.EQUALS]: "Is exactly",
  [ConditionTypeEnum.NOT_EQUALS]: "Is not",
  [ConditionTypeEnum.CONTAINS]: "Contains",
  [ConditionTypeEnum.NOT_CONTAINS]: "Does not contain",
  [ConditionTypeEnum.MATCHES_REGEX]: "Matches pattern",
  [ConditionTypeEnum.IS_EMPTY]: "Is empty",
  [ConditionTypeEnum.IS_NOT_EMPTY]: "Is not empty",
  [ConditionTypeEnum.GREATER_THAN]: "Is greater than",
  [ConditionTypeEnum.LESS_THAN]: "Is less than",
  [ConditionTypeEnum.BETWEEN]: "Is between",
  [ConditionTypeEnum.LENGTH_EQUALS]: "Has length",
  [ConditionTypeEnum.LENGTH_GREATER_THAN]: "Is longer than",
  [ConditionTypeEnum.LENGTH_LESS_THAN]: "Is shorter than",
};

const WITHOUT_VALUE: ConditionTypeEnum[] = [
  ConditionTypeEnum.IS_EMPTY,
  ConditionTypeEnum.IS_NOT_EMPTY,
];

const LENGTHS: ConditionTypeEnum[] = [
  ConditionTypeEnum.LENGTH_EQUALS,
  ConditionTypeEnum.LENGTH_GREATER_THAN,
  ConditionTypeEnum.LENGTH_LESS_THAN,
];

export const needsValue = (condition: ConditionTypeEnum): boolean =>
  !WITHOUT_VALUE.includes(condition);

export const needsSecondValue = (condition: ConditionTypeEnum): boolean =>
  condition === ConditionTypeEnum.BETWEEN;

export const isLength = (condition: ConditionTypeEnum): boolean => LENGTHS.includes(condition);

/** A count of characters, or a {{variable}} that will be one by the time the step executes. */
export const isLengthValue = (text: string): boolean =>
  /^\s*\d+\s*$/.test(text) || text.includes("{{");

/**
 * All of them. The extract pattern narrows the read before the condition sees it, so what is being
 * compared is the captured group rather than the whole block - which is what makes "the total is
 * over 100" a sensible thing to ask of a screen.
 */
export const SEARCH_TEXT_CONDITION_TYPES = [
  ConditionTypeEnum.CONTAINS,
  ConditionTypeEnum.NOT_CONTAINS,
  ConditionTypeEnum.EQUALS,
  ConditionTypeEnum.NOT_EQUALS,
  ConditionTypeEnum.MATCHES_REGEX,
  ConditionTypeEnum.IS_EMPTY,
  ConditionTypeEnum.IS_NOT_EMPTY,
  ConditionTypeEnum.GREATER_THAN,
  ConditionTypeEnum.LESS_THAN,
  ConditionTypeEnum.BETWEEN,
  ConditionTypeEnum.LENGTH_EQUALS,
  ConditionTypeEnum.LENGTH_GREATER_THAN,
  ConditionTypeEnum.LENGTH_LESS_THAN,
] as const;

export const conditionOptions = (types: readonly ConditionTypeEnum[]) =>
  types.map((value) => ({ label: CONDITION_LABELS[value], value }));
