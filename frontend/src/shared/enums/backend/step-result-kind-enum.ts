export const StepResultKindEnum = {
  LOCATION: "LOCATION",
  VALUE: "VALUE",
} as const;

export type StepResultKindEnum =
  (typeof StepResultKindEnum)[keyof typeof StepResultKindEnum];
