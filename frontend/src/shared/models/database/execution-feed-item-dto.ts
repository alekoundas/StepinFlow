import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";
import type { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";

/** One execution as the executions feed shows it, with the names it would otherwise look up. */
export interface ExecutionFeedItemDto {
  id: number;
  flowId: number;
  flowName: string;

  createdOn: string;
  completedAt?: string | null;

  status: ExecutionStatusEnum;
  historyLevel: ExecutionHistoryLevelEnum;
  stepCount: number;

  errorMessage: string;

  /** The step that threw, when the execution errored. Null otherwise, or once that step is deleted. */
  errorStepName?: string | null;

  /** Where its screenshots are. Null when it kept none. */
  screenshotFolderPath?: string | null;
  commitSha?: string | null;
  hasUncommittedChanges?: boolean | null;
}
