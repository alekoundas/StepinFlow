export const BrowserTypeEnum = {
  ANY: "ANY",
  CHROME: "CHROME",
  EDGE: "EDGE",
  FIREFOX: "FIREFOX",
} as const;

export type BrowserTypeEnum =
  (typeof BrowserTypeEnum)[keyof typeof BrowserTypeEnum];
