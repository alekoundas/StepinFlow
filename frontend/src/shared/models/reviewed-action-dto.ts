import type { ReviewChoiceEnum } from "@/shared/enums/backend/review-choice-enum";

/** What the tester decided about one recorded action, in Review. */
export interface ReviewedActionDto {
  actionIndex: number;
  choice: ReviewChoiceEnum;

  // The element's rectangle as reviewed, in the main area's pixels. Null for an action with none.
  elementX?: number | null;
  elementY?: number | null;
  elementWidth?: number | null;
  elementHeight?: number | null;

  /** For typed text that becomes an input: its name. */
  inputName?: string | null;

  /** For an input: never written to a file, never shown to a model. */
  isSecret: boolean;
}
