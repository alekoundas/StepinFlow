export const ImageSearchModeEnum = {
  FIND_ONCE: "FIND_ONCE",
  WAIT_UNTIL_FOUND: "WAIT_UNTIL_FOUND",
  WAIT_UNTIL_GONE: "WAIT_UNTIL_GONE",
} as const;

export type ImageSearchModeEnum =
  (typeof ImageSearchModeEnum)[keyof typeof ImageSearchModeEnum];
