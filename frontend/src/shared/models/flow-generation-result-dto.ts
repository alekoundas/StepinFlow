import type { FlowScriptErrorDto, FlowScriptImportResultDto } from "@/shared/models/flow-script-import-result-dto";
import type { ProposedCheckDto } from "@/shared/models/proposed-check-dto";

/**
 * What a recording's draft saved, and what the Check page shows of it: the plain draft, the
 * model's, or the plain one when the model's answer never read cleanly or the model could not be
 * reached, and why. A revision asked for on the Check page answers with one too.
 */
export interface FlowGenerationResultDto {
  import: FlowScriptImportResultDto;

  /** True when the model's answer was saved. */
  isModelDraft: boolean;

  /** Why the model's answer was not saved. Empty when it was, and for a plain draft the tester chose. */
  reason: string;

  /** What the model says a tester should check. */
  notes: string[];

  /** The checks the model added, for the tester to keep or drop. In the saved flow until dropped. */
  proposedChecks: ProposedCheckDto[];

  /** Why the last answer was refused, when it was not saved because of it. */
  refused: FlowScriptErrorDto[];

  attempts: number;

  /** The first request's message, exactly as the model was sent it. */
  request: string;

  /** The script the model was given: what it changed is told against this. Empty when no model was asked. */
  draft: string;

  /** The flow as saved, as script text. Empty when nothing was saved. */
  script: string;

  /** The saved flow's steps, without the Success and Failure branches that hold them. */
  stepCount: number;
  inputCount: number;

  /** The saved flow's pictures, each once however many steps search for it. */
  templateCount: number;
}
