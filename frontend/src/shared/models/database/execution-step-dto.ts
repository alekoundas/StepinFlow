import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";

/**
 * One step, as it ran. Ordered by sequence and indented by depth it reads as a tree, which is
 * exactly how the run panel draws it - no joins, no walking back up.
 */
export interface ExecutionStepDto {
  id: number;

  // Where it sits in the run
  sequence: number;
  parentSequence?: number | null;
  depth: number;
  loopPass?: number | null;

  name: string;
  flowStepType: FlowStepTypeEnum;
  outcome: StepOutcomeEnum;
  startedOn: string;
  durationMilliseconds: number;

  resultLocationX?: number | null;
  resultLocationY?: number | null;
  matchIndex?: number | null;
  matchCount?: number | null;

  // What came back
  value?: string | null;
  message?: string | null;

  // SYSTEM_COMMAND
  exitCode?: number | null;
  error?: string | null;
  command?: string | null;

  resultImagePath?: string | null;

  executionId: number;
  flowStepId?: number | null;
}
