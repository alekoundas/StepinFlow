export const ResultSourceEnum = {
  STDOUT: "STDOUT",
  STDERR: "STDERR",
  COMBINED: "COMBINED",
  EXIT_CODE: "EXIT_CODE",
} as const;

export type ResultSourceEnum =
  (typeof ResultSourceEnum)[keyof typeof ResultSourceEnum];
