/**
 * Steps grouped by what they do to the machine, which is what the tree colours by. Control is
 * deliberately neutral: WAIT and LOOP are the most common steps in any flow, and colouring them
 * would drown out the steps that actually act.
 *
 * The one list of step types: the tree, the forms and the "add a step" picker all read it.
 */
export const FlowStepGroupEnum = {
  CONTROL: "CONTROL",
  INPUT: "INPUT",
  WINDOW: "WINDOW",
  PERCEPTION: "PERCEPTION",
  SYSTEM: "SYSTEM",
  BRANCH: "BRANCH",
} as const;

export type FlowStepGroupEnum =
  (typeof FlowStepGroupEnum)[keyof typeof FlowStepGroupEnum];
