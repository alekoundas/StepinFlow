import type { WindowMatchDto } from "@/shared/models/window-match-dto";

export interface WindowMatchTestResultDto {
  /** In z-order. The count matters: the first one is the one that runs. */
  matches: WindowMatchDto[];
}
