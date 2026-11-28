import type { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";

/** A check's rule, tested against the words read on what the gesture pressed. */
export interface RecordingGestureRuleDto {
  pressIndex: number;
  conditionType: ConditionTypeEnum;
  text: string;
  textEnd: string;
}
