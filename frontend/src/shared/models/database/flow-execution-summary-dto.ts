import type { ExecutionStatusEnum } from "@/shared/enums/backend/execution/execution-status-enum";

/** One flow's run history, in the shape the executions list reads it. */
export interface FlowExecutionSummaryDto {
  flowId: number;
  flowName: string;
  isSubFlow: boolean;

  runCount: number;
  completedCount: number;

  lastRunOn?: string | null;
  lastStatus?: ExecutionStatusEnum | null;

  /** Oldest first, so a row of bars reads left to right like a timeline. */
  recentOutcomes: ExecutionStatusEnum[];
}
