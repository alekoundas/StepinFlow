export interface ReadTextTestResultDto {
  isResolved: boolean;
  errorMessage?: string;

  // Everything Windows read in the area, so a near miss is visible.
  text: string;

  isMatch: boolean;

  // The value the step would hand to later steps, after extraction.
  resultValue: string;
}
