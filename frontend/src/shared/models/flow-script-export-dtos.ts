/** Where an export landed. */
export interface FlowScriptExportResultDto {
  scriptPath: string;

  /** The folder holding the template images, beside the script. Empty when there were none. */
  templateFolderPath: string;
  templateCount: number;
  script: string;
}

/** A .sflw file in a folder, and how many templates sit in the folder beside it. */
export interface FlowScriptFileDto {
  path: string;
  fileName: string;
  modifiedAt: string;
  templateCount: number;
}

/** The app's export folder, and the scripts already in it, newest first. */
export interface FlowScriptExportFolderDto {
  folderPath: string;
  scripts: FlowScriptFileDto[];
}
