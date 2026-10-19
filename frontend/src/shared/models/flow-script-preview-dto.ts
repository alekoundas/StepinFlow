import type { FlowScriptErrorDto } from "@/shared/models/flow-script-import-result-dto";

/** An input as a script declares it. A secret has no value in the file. */
export interface FlowScriptInputDto {
  name: string;
  defaultValue: string;
  isSecret: boolean;
}

/** What an import meets: the flow with the file's Id, and whether another flow has its name. */
export interface FlowScriptImportTargetDto {
  /** Null when the file is a new flow. */
  flowId: number | null;
  flowName: string;
  executionCount: number;

  /** Why the name cannot be used. Null when it is free. */
  nameTaken: string | null;
}

/** What importing a .sflw file would do, with nothing saved. */
export interface FlowScriptPreviewDto {
  scriptPath: string;
  script: string;
  templateFolderPath: string;

  /** Any of these and nothing can be imported. */
  errors: FlowScriptErrorDto[];
  warnings: FlowScriptErrorDto[];

  flowName: string;
  publicId: string;
  description: string;

  /** Steps, without the Success and Failure branches that hold them. */
  stepCount: number;
  inputs: FlowScriptInputDto[];

  /** Each picture once, however many steps search for it. */
  templateNames: string[];

  /** Named by the file but not in the folder beside it. Imported without a picture. */
  missingTemplates: string[];

  target: FlowScriptImportTargetDto;

  /** The flow it replaces as it is now. Empty for a new flow. */
  replacedScript: string;
}
