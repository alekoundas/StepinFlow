import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";
import type { ExecutionStepDto } from "@/shared/models/database/execution-step-dto";

/** One run of a flow. ExecutionSteps is filled only when a single run is opened. */
export interface ExecutionDto {
  id: number;
  createdOn: string;
  completedAt?: string | null;

  status: ExecutionStatusEnum;
  historyLevel: ExecutionHistoryLevelEnum;
  stepCount: number;

  /** The failure that ended the run - a step that failed into a Failure branch is not this. */
  errorFlowStepId?: number | null;
  errorMessage: string;

  /** The shape of the flow at the time - a run stops being replayable once this stops matching. */
  flowStructureHash: string;

  flowId: number;

  executionSteps: ExecutionStepDto[];
}
