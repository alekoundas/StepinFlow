export const ScalesWithEnum = {
  DPI: "DPI",
  AREA: "AREA",
} as const;

export type ScalesWithEnum =
  (typeof ScalesWithEnum)[keyof typeof ScalesWithEnum];
