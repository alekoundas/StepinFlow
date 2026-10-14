export const RecordingStateEnum = {
  NONE: "NONE", // Nothing started, or the last recording was discarded
  RECORDING: "RECORDING",
  PAUSED: "PAUSED",
  PROCESSING: "PROCESSING", // Stopped, and what it recorded is being worked through, stage by stage
  READY: "READY", // Stopped and worked through, kept for the review
} as const;

export type RecordingStateEnum =
  (typeof RecordingStateEnum)[keyof typeof RecordingStateEnum];
