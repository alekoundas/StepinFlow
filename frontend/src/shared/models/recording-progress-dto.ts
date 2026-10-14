import type { RecordingStateEnum } from "@/shared/enums/backend/recording-state-enum";
import type { RecordingStageEnum } from "@/shared/enums/backend/recording-stage-enum";
import type { RecordingLiveActionDto } from "@/shared/models/recording-live-action-dto";

/**
 * Where a recording is, from the first click to the review being ready. Sent whole on every
 * change as RECORDING_PROGRESS.
 */
export interface RecordingProgressDto {
  state: RecordingStateEnum;

  /** Since the recording started, pauses included, up to the stop. */
  elapsedMilliseconds: number;

  /** What the tester did so far. Pauses are not counted. */
  actionCount: number;

  /** The newest actions, oldest first. */
  recentActions: RecordingLiveActionDto[];

  /** What a stopped recording is being worked through. NONE unless PROCESSING. */
  stage: RecordingStageEnum;

  /** Which stage, from 1, of stageCount. Both 0 unless PROCESSING. */
  stageNumber: number;
  stageCount: number;

  /** How far the stage has got, of stageTotal. Null when it cannot be counted ahead. */
  stageDone: number;
  stageTotal?: number | null;

  /** Set when a stage failed. The recording is still READY, with less in it. */
  errorMessage?: string | null;
}
