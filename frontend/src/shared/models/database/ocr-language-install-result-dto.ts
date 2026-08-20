export interface OcrLanguageInstallResultDto {
  // Windows is still downloading. The list has to be polled for the result.
  isRunning: boolean;

  errorMessage?: string;
}
