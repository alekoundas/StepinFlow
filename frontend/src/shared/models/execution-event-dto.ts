import type { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { ExecutionEventTypeEnum } from "@/shared/enums/backend/execution/execution-event-type-enum";
import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { StepOutcomeEnum } from "@/shared/enums/backend/execution/step-outcome-enum";

/** What the runner reports as it walks. The only thing the page needs to follow a run live. */
export interface ExecutionEventDto {
  type: ExecutionEventTypeEnum;
  executionId: number;

  flowStepId?: number | null;
  name: string;
  flowStepType: FlowStepTypeEnum;

  sequence?: number | null;
  parentSequence?: number | null;
  depth?: number | null;
  loopPass?: number | null;

  outcome?: StepOutcomeEnum | null;
  durationMilliseconds?: number | null;
  resultLocationX?: number | null;
  resultLocationY?: number | null;
  matchIndex?: number | null;
  matchCount?: number | null;
  value?: string | null;
  message?: string | null;

  status?: ExecutionStatusEnum | null;
  errorMessage?: string | null;
}
