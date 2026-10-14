import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";

/** A stopped recording once worked through: what the review opens on. */
export interface RecordingResultDto {
  actions: RecordedActionDto[];

  /** What each click and drag inside the main area hit. Empty with no main area, or when finding it failed. */
  clickedElements: ClickedElementDto[];
}
