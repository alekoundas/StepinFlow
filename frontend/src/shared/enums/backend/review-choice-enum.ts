// What the tester said a recorded action becomes, in Review.
export const ReviewChoiceEnum = {
  KEEP: "KEEP", // As it was: a key pressed, a scroll, a click outside the main area kept
  WAIT_AND_CLICK: "WAIT_AND_CLICK", // Wait for the element, then click wherever it was found
  CLICK_AT_SPOT: "CLICK_AT_SPOT", // Click the place it was clicked, without looking for anything
  INPUT: "INPUT", // Typed text that becomes an input, with what was typed as its default
  TYPE_AS_WRITTEN: "TYPE_AS_WRITTEN", // Typed text written into the flow as it is
  LEAVE_OUT: "LEAVE_OUT",

  // A check marked while recording
  CHECK_THERE: "CHECK_THERE", // Wait until the element is there
  CHECK_GONE: "CHECK_GONE", // Wait until the element has gone

  // A step that is not a click, marked while recording
  EXECUTE_COMMAND: "EXECUTE_COMMAND", // A command, written in Review
  LAUNCH_APP: "LAUNCH_APP", // An application opened, its program written in Review
  TYPE_INPUT: "TYPE_INPUT", // An input's value typed, the input named in Review
} as const;

export type ReviewChoiceEnum = (typeof ReviewChoiceEnum)[keyof typeof ReviewChoiceEnum];
