// A press while recording that marks something rather than clicks.
export const RecordingGestureEnum = {
  CHECK: "CHECK", // Something to check at that spot
  SUBSTITUTE: "SUBSTITUTE", // Something done there instead of a click
} as const;

export type RecordingGestureEnum = (typeof RecordingGestureEnum)[keyof typeof RecordingGestureEnum];
