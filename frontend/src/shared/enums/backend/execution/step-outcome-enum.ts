export const StepOutcomeEnum = {
  SUCCESS: "SUCCESS",
  FAILURE: "FAILURE",
} as const;

export type StepOutcomeEnum =
  (typeof StepOutcomeEnum)[keyof typeof StepOutcomeEnum];
