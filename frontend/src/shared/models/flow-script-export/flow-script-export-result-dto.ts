/** Where an export landed. */
export interface FlowScriptExportResultDto {
  scriptPath: string;

  /** The folder holding the template images, beside the script. Empty when there were none. */
  templateFolderPath: string;
  templateCount: number;
  script: string;
}
