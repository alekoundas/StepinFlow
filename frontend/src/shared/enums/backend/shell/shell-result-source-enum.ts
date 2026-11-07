export const ShellResultSourceEnum = {
  STDOUT: "STDOUT",
  STDERR: "STDERR",
  COMBINED: "COMBINED",
  EXIT_CODE: "EXIT_CODE",
} as const;

export type ShellResultSourceEnum =
  (typeof ShellResultSourceEnum)[keyof typeof ShellResultSourceEnum];
