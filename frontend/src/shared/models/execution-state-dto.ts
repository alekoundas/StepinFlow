import type { RunStateEnum } from "@/shared/enums/backend/execution/run-state-enum";

/**
 * What the engine is doing right now. Read on mount, because the engine outlives the page and a
 * run started before you navigated away is still going.
 */
export interface ExecutionStateDto {
  state: RunStateEnum;
  isRunning: boolean;
  executionId: number;
  flowId: number;
}
