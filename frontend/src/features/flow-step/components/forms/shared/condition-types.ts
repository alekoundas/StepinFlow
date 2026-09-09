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
};

const WITHOUT_VALUE: ConditionTypeEnum[] = [
  ConditionTypeEnum.IS_EMPTY,
  ConditionTypeEnum.IS_NOT_EMPTY,
];

export const needsValue = (condition: ConditionTypeEnum): boolean =>
  !WITHOUT_VALUE.includes(condition);

export const needsSecondValue = (condition: ConditionTypeEnum): boolean =>
  condition === ConditionTypeEnum.BETWEEN;

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
] as const;

export const conditionOptions = (types: readonly ConditionTypeEnum[]) =>
  types.map((value) => ({ label: CONDITION_LABELS[value], value }));
