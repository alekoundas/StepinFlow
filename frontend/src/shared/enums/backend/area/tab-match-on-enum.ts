export const TabMatchOnEnum = {
  TITLE: "TITLE",
  URL: "URL",
} as const;

export type TabMatchOnEnum =
  (typeof TabMatchOnEnum)[keyof typeof TabMatchOnEnum];
