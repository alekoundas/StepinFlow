export interface RunCommandTestResultDto {
  isSuccess: boolean;
  errorMessage?: string;

  // What actually ran, after the preset and its parameter were applied.
  resolvedCommand: string;

  exitCode: number;
  durationMilliseconds: number;
  standardOutput: string;
  standardError: string;

  // The value the step would hand to later steps, after extraction.
  resultValue: string;
}
