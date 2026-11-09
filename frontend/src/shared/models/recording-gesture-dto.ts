import type { RecordingGestureEnum } from "@/shared/enums/backend/recording-gesture-enum";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";
import type { ReviewedActionDto } from "@/shared/models/reviewed-action-dto";
import type { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";

/** A gesture the recording paused itself at, to ask what it was for. In RECORDING_PROGRESS until answered. */
export interface RecordingGestureAskDto {
  /** The press it was, which the answer is kept by. */
  pressIndex: number;
  gesture: RecordingGestureEnum;

  /** Whether what it pressed has been looked for, so the window asks for it again. */
  isElementFound: boolean;
}

/** The gesture being asked about: the main area just before the press, where it was pressed, and what it pressed once found. */
export interface RecordingGestureDto {
  pressIndex: number;
  gesture: RecordingGestureEnum;

  /** The main area's screenshot, base64 PNG. Null when the press kept none. */
  image?: string | null;

  /** The main area's bounds on the screen, and its DPI. */
  x: number;
  y: number;
  width: number;
  height: number;
  dpi: number;

  /** Where it was pressed, on the screen. */
  locationX: number;
  locationY: number;

  /** What the press hit and the words read on it. Null until found, and for a step that is not a click. */
  element?: ClickedElementDto | null;

  /** Whether looking for it has finished, found or not. */
  isElementFound: boolean;
}

/** What the tester said a gesture was for, or no answer, which leaves it for Review. */
export interface RecordingGestureDecisionDto {
  pressIndex: number;
  answer: ReviewedActionDto | null;
}

/** A check's rule, tested against the words read on what the gesture pressed. */
export interface RecordingGestureRuleDto {
  pressIndex: number;
  conditionType: ConditionTypeEnum;
  text: string;
  textEnd: string;
}
