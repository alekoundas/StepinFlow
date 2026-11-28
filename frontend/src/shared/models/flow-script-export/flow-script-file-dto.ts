/** A .sflw file in a folder, and how many templates sit in the folder beside it. */
export interface FlowScriptFileDto {
  path: string;
  fileName: string;
  modifiedAt: string;
  templateCount: number;
}
