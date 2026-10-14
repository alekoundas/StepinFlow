// What a stopped recording is being worked through, in order.
export const RecordingStageEnum = {
  NONE: "NONE", // Not processing
  FINDING_ELEMENTS: "FINDING_ELEMENTS", // What each click hit, by the detection model and OCR
  GROUPING_CLICKS: "GROUPING_CLICKS", // Which clicks hit the same picture
} as const;

export type RecordingStageEnum =
  (typeof RecordingStageEnum)[keyof typeof RecordingStageEnum];
