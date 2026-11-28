import type { RecordingGestureEnum } from "@/shared/enums/backend/recording-gesture-enum";
import type { ClickedElementDto } from "@/shared/models/clicked-element-dto";

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
