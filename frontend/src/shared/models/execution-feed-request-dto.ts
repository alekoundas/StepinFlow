import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";

/** A page of the executions feed: every flow's executions, or one flow's, newest first. */
export interface ExecutionFeedRequestDto {
  /** One flow's executions only. Null for every flow's. */
  flowId: number | null;

  /** Only executions older than this one, which is where the page before ended. Null for the newest. */
  beforeId: number | null;

  /** Only executions that ended one of these ways. Empty for all of them. */
  statuses: ExecutionStatusEnum[];
}
