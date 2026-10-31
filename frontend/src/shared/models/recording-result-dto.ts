import type { RecordedActionDto } from "@/shared/models/database/recorded-action-dto";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";
import type { ClickGroupDto } from "@/shared/models/click-group-dto";
import type { PressedElementDto } from "@/shared/models/pressed-element-dto";
import type { ScreenChangeDto } from "@/shared/models/screen-change-dto";
import type { ScreenDescriptionDto } from "@/shared/models/screen-description-dto";

/** A stopped recording once worked through: what the review opens on. */
export interface RecordingResultDto {
  actions: RecordedActionDto[];

  /** What each click and drag inside the main area hit. Empty with no main area, or when finding it failed. */
  clickedElements: ClickedElementDto[];

  /** The clicks that hit the same picture. Empty when none did, or grouping failed. */
  clickGroups: ClickGroupDto[];

  /** What changed on the screen between one press and the next, and from the last to the screen the recording stopped on. Empty when reading failed. */
  screenChanges: ScreenChangeDto[];

  /** Every line OCR read on the main area, each once, in the order first read. */
  screenLines: string[];

  /** What each press landed on, as its application told Windows. Only presses an application answered for. */
  pressedElements: PressedElementDto[];

  /** What the local model saw at the presses worth a look. Filled only as the recording goes to a model. */
  screenDescriptions: ScreenDescriptionDto[];
}
