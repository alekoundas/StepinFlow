import type { RecordingGestureEnum } from "@/shared/enums/backend/recording-gesture-enum";

/** A gesture the recording paused itself at, to ask what it was for. In RECORDING_PROGRESS until answered. */
export interface RecordingGestureAskDto {
  /** The press it was, which the answer is kept by. */
  pressIndex: number;
  gesture: RecordingGestureEnum;

  /** Whether what it pressed has been looked for, so the window asks for it again. */
  isElementFound: boolean;
}
