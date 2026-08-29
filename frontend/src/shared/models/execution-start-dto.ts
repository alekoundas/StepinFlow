import type { ExecutionHistoryLevelEnum } from "@/shared/enums/backend/execution/execution-history-level-enum";

export interface ExecutionStartDto {
  flowId: number;
  historyLevel: ExecutionHistoryLevelEnum;
  breakpoints: number[];
}
