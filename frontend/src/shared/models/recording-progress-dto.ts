import type { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import type { RecordingLiveActionDto } from "@/shared/models/recording-live-action-dto";

/**
 * Where a recording is. Sent whole on every change as RECORDING_PROGRESS, and read when a page
 * opens, so a page that opens late misses nothing.
 */
export interface RecordingProgressDto {
  state: RecordingStateEnum;

  /** Since the recording started, pauses included, as of this message. */
  elapsedMilliseconds: number;

  /** What the tester did so far. Pauses are not counted. */
  actionCount: number;

  /** The newest actions, oldest first. */
  recentActions: RecordingLiveActionDto[];
}
