/**
 * A check a model added to a recording, which the Check page offers to keep or drop: one that
 * nothing moves to, and that the script it was given did not have.
 */
export interface ProposedCheckDto {
  name: string;

  /** Its line in the saved script, from 1. 0 when it could not be found. */
  line: number;

  /** That line, as the script has it. */
  text: string;

  /** The step whose branch it sits in. Empty at the top of the flow. */
  under: string;

  /**
   * Whether a line OCR read on the recording's screenshots satisfies its words. Null for a check of
   * a picture, or of words an execution fills in.
   */
  isReadOnScreen: boolean | null;
}
