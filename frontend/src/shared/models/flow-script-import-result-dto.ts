import type { FlowValidationResultDto } from "@/shared/models/database/flow-validation-result-dto";

export interface FlowScriptErrorDto {
  line: number;
  column: number;
  message: string;

  /** The message is for reading; these are for deciding what to do about it. */
  code: string;
  severity: string;
}

/**
 * What an import did, or why it did nothing. A failed import leaves the flow exactly as it was, so
 * an error list and an untouched flow are the same outcome.
 */
export interface FlowScriptImportResultDto {
  isSuccess: boolean;
  flowId: number;
  flowName: string;
  stepCount: number;
  templateCount: number;

  /** Templates the script named that were not in the folder beside it. */
  missingTemplates: string[];
  errors: FlowScriptErrorDto[];

  /** What the file got away with, on an import that worked as well as one that did not. */
  warnings: FlowScriptErrorDto[];

  /** The imported flow as the validator sees it once saved. Its errors do not undo the import. */
  validation?: FlowValidationResultDto | null;
}
