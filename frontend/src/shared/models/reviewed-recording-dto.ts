import type { ReviewedActionDto } from "@/shared/models/reviewed-action-dto";

/** A stopped recording as the tester reviewed it: what the draft is built from. */
export interface ReviewedRecordingDto {
  /** One per action Review listed, in order. A pause is not listed. */
  actions: ReviewedActionDto[];
}
