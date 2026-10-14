// What a stopped recording is being worked through, in order.
export const RecordingStageEnum = {
  NONE: "NONE", // Not processing
  FINDING_ELEMENTS: "FINDING_ELEMENTS", // What each click hit, by the detection model and OCR
} as const;

export type RecordingStageEnum =
  (typeof RecordingStageEnum)[keyof typeof RecordingStageEnum];
