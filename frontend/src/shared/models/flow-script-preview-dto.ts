import type { FlowScriptErrorDto } from "@/shared/models/flow-script-error-dto";
import type { FlowScriptInputDto } from "@/shared/models/flow-script-input-dto";
import type { FlowScriptImportTargetDto } from "@/shared/models/flow-script-import-target-dto";

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

  /** Named by the file but not a PNG in the folder beside it. Imported without a picture. */
  missingTemplates: string[];

  target: FlowScriptImportTargetDto;

  /** The flow it replaces as it is now. Empty for a new flow. */
  replacedScript: string;
}
