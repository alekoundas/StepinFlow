export const ShellCommandModeEnum = {
  CUSTOM: "CUSTOM",
  KILL_PROCESS: "KILL_PROCESS",
  IS_PROCESS_RUNNING: "IS_PROCESS_RUNNING",
  READ_CLIPBOARD: "READ_CLIPBOARD",
  WRITE_CLIPBOARD: "WRITE_CLIPBOARD",
  CHECK_INTERNET: "CHECK_INTERNET",
  SHUTDOWN_IN: "SHUTDOWN_IN",
  CANCEL_SHUTDOWN: "CANCEL_SHUTDOWN",
  LAUNCH_APP: "LAUNCH_APP",
} as const;

export type ShellCommandModeEnum =
  (typeof ShellCommandModeEnum)[keyof typeof ShellCommandModeEnum];
