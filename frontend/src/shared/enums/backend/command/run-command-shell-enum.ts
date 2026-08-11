export const RunCommandShellEnum = {
  CMD: "CMD",
  POWERSHELL: "POWERSHELL",
} as const;

export type RunCommandShellEnum =
  (typeof RunCommandShellEnum)[keyof typeof RunCommandShellEnum];
