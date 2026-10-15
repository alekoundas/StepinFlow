import type { FlowScriptErrorDto, FlowScriptImportResultDto } from "@/shared/models/flow-script-import-result-dto";

/**
 * What a generation saved: the model's draft, or the plain one when no answer read cleanly or the
 * model could not be reached, and why.
 */
export interface FlowGenerationResultDto {
  import: FlowScriptImportResultDto;

  /** True when the model's answer was saved, false when the plain draft was. */
  isModelDraft: boolean;

  /** Why the plain draft was saved instead. Empty for the model's draft. */
  reason: string;

  /** What the model says a tester should check. */
  notes: string[];

  /** Why the last answer was refused, when the plain draft was saved because of it. */
  refused: FlowScriptErrorDto[];

  attempts: number;

  /** The first request's message, exactly as the model was sent it. */
  request: string;
}
