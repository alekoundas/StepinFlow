export const SearchModeEnum = {
  FIND_BEST: "FIND_BEST",
  FIND_ALL: "FIND_ALL",
  WAIT_UNTIL_FOUND: "WAIT_UNTIL_FOUND",
  WAIT_UNTIL_NOT_FOUND: "WAIT_UNTIL_NOT_FOUND",
} as const;

export type SearchModeEnum =
  (typeof SearchModeEnum)[keyof typeof SearchModeEnum];
