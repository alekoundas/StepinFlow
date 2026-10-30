import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";
import type { ClickGroupDto } from "@/shared/models/click-group-dto";
import type { ScreenChangeDto } from "@/shared/models/screen-change-dto";

/** A stopped recording once worked through: what the review opens on. */
export interface RecordingResultDto {
  actions: RecordedActionDto[];

  /** What each click and drag inside the main area hit. Empty with no main area, or when finding it failed. */
  clickedElements: ClickedElementDto[];

  /** The clicks that hit the same picture. Empty when none did, or grouping failed. */
  clickGroups: ClickGroupDto[];

  /** What changed on the screen between one press and the next. Empty with fewer than two presses, or when reading failed. */
  screenChanges: ScreenChangeDto[];
}
