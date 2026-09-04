import type { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";
import type { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import type { CursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";
import type { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";

/**
 * One thing the user did, after a press and a release have been folded into a click and a burst
 * of typing into a single entry.
 *
 * Deliberately not a step. What a click should become is the wizard question.
 */
export interface RecordedActionDto {
  index: number;
  kind: RecordedActionKindEnum;
  summary: string;
  windowTitle?: string | null;

  /** Key into the session screenshot store, when one was captured. */
  screenshotIndex?: number | null;

  locationX: number;
  locationY: number;
  locationEndX: number;
  locationEndY: number;
  cursorButtonType?: CursorButtonTypeEnum | null;

  /** Single or double, measured from how close the two presses were. */
  cursorButtonActionType?: CursorButtonActionTypeEnum | null;

  scrollDirection?: CursorScrollDirectionTypeEnum | null;
  scrollAmount: number;

  text?: string | null;

  /** How many times the keyboard repeated a held key. Not replayed; recorded so it is not lost. */
  repeatCount: number;

  /** How long the key was down. */
  holdMilliseconds: number;
  pauseMilliseconds: number;
}
