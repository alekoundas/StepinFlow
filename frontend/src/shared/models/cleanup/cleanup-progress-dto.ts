import type { CleanupStageEnum } from "@/shared/enums/backend/cleanup-stage-enum";

/** How a cleanup is going. Done and total count the stage's own work: steps, then flows, then pages. */
export interface CleanupProgressDto {
  stage: CleanupStageEnum;
  done: number;
  total: number;

  isDone: boolean;
  error: string;
}
