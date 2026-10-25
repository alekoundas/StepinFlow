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

  /** The run folder under the history path, shared by everything this run wrote. */
  screenshotFolderName?: string | null;

  /** The shape of the flow at the time - a run stops being replayable once this stops matching. */
  flowStructureHash: string;

  /** The repository's last commit as it started. Null when there was none or it could not be read. */
  commitSha?: string | null;

  /** The flow or a sub-flow it calls had changes not committed yet. Null when unknown. */
  hasUncommittedChanges?: boolean | null;

  /** Filled only when a single run is opened, and only when the commit cannot say what executed. */
  script?: string | null;

  flowId: number;

  executionSteps: ExecutionStepDto[];
}
