export const ShellTypeEnum = {
  CMD: "CMD",
  POWERSHELL: "POWERSHELL",
} as const;

export type ShellTypeEnum =
  (typeof ShellTypeEnum)[keyof typeof ShellTypeEnum];
