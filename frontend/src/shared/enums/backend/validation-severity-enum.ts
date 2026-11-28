export const ValidationSeverityEnum = {
  ERROR: "ERROR",
  WARNING: "WARNING",
  INFO: "INFO", // Runs as meant, but worth knowing, quieter than a warning
} as const;

export type ValidationSeverityEnum =
  (typeof ValidationSeverityEnum)[keyof typeof ValidationSeverityEnum];
