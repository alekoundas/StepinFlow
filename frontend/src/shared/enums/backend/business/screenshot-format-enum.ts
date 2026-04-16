export const ScreenshotFormatEnum = {
  RAW: "RAW",
  JPEG: "JPEG",
  PNG: "PNG",
} as const;

export type ScreenshotFormatEnum =
  (typeof ScreenshotFormatEnum)[keyof typeof ScreenshotFormatEnum];
