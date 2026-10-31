export const RecordedActionKindEnum = {
  CLICK: "CLICK",
  DRAG: "DRAG",
  SCROLL: "SCROLL",
  TYPING: "TYPING",
  KEY_COMBINATION: "KEY_COMBINATION",
  KEY_HOLD: "KEY_HOLD",
  KEY_RELEASE: "KEY_RELEASE",
  PAUSE: "PAUSE",
  CHECK: "CHECK", // A press marking something to check at that spot, the gesture's
  SUBSTITUTE: "SUBSTITUTE", // A press marking a step that is not a click, the gesture's
  STAGE: "STAGE", // Where the tester said a stage of the test begins, the hotkey's
} as const;

export type RecordedActionKindEnum =
  (typeof RecordedActionKindEnum)[keyof typeof RecordedActionKindEnum];
