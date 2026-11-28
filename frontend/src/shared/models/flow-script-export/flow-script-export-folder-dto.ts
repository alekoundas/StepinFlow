import type { FlowScriptFileDto } from "@/shared/models/flow-script-export/flow-script-file-dto";

/** The app's export folder, and the scripts already in it, newest first. */
export interface FlowScriptExportFolderDto {
  folderPath: string;
  scripts: FlowScriptFileDto[];
}
