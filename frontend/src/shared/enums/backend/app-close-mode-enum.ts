export const AppCloseModeEnum = {
  /** Leave it open. For a flow that did not start the application in the first place. */
  LEAVE: "LEAVE",

  /** Ask the window to close, the way a person clicking the X would. */
  CLOSE_WINDOW: "CLOSE_WINDOW",

  /** Kill the process. For an application that will not close on being asked. */
  KILL_PROCESS: "KILL_PROCESS",
} as const;

export type AppCloseModeEnum =
  (typeof AppCloseModeEnum)[keyof typeof AppCloseModeEnum];
