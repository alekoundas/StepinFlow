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

/** Reading a whole block of text against a number is not a comparison, so only these apply. */
export const READ_TEXT_CONDITION_TYPES = [
  ConditionTypeEnum.CONTAINS,
  ConditionTypeEnum.EQUALS,
  ConditionTypeEnum.MATCHES_REGEX,
] as const;

export const conditionOptions = (types: readonly ConditionTypeEnum[]) =>
  types.map((value) => ({ label: CONDITION_LABELS[value], value }));
