import type { RecordedActionKindEnum } from "@/shared/enums/backend/recorded-action-kind-enum";

/**
 * One action as the live list says it while recording. Typed text is counted rather than shown,
 * so a password never appears on screen.
 */
export interface RecordingLiveActionDto {
  index: number;
  kind: RecordedActionKindEnum;
  line: string;

  /** Since the recording started. */
  elapsedMilliseconds: number;
}
