import type { ReviewedActionDto } from "@/shared/models/reviewed-action-dto";

/** What the tester said a gesture was for, or no answer, which leaves it for Review. */
export interface RecordingGestureDecisionDto {
  pressIndex: number;
  answer: ReviewedActionDto | null;
}
