export const RecordingStateEnum = {
  NONE: "NONE", // Nothing started, or the last recording was discarded
  RECORDING: "RECORDING",
  PAUSED: "PAUSED",
  STOPPED: "STOPPED", // Ended, and what it recorded is kept for the review
} as const;

export type RecordingStateEnum =
  (typeof RecordingStateEnum)[keyof typeof RecordingStateEnum];
