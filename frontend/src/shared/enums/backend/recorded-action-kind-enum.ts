export const RecordedActionKindEnum = {
  CLICK: "CLICK",
  DRAG: "DRAG",
  SCROLL: "SCROLL",
  TYPING: "TYPING",
  KEY_COMBINATION: "KEY_COMBINATION",
  PAUSE: "PAUSE",
} as const;

export type RecordedActionKindEnum =
  (typeof RecordedActionKindEnum)[keyof typeof RecordedActionKindEnum];
