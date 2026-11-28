import type { CleanupProgressDto } from "@/shared/models/cleanup/cleanup-progress-dto";

/** What the deletes left for the cleanup, and how a cleanup going now is doing. */
export interface CleanupStatusDto {
  executionCount: number;
  flowCount: number;

  /** About what cleaning up gives back: the deleted steps' share of the database, their screenshots, the pages already free. */
  bytes: number;

  /** Enough to show the card on Home for. */
  isWorthCleaning: boolean;

  /** Null while none is going. */
  progress: CleanupProgressDto | null;
}
