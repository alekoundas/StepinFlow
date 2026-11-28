import type { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";

/** One flow a branch switch touches. */
export interface RepositoryBranchSwitchFlowDto {
  /** Null for a flow new on the branch until it is imported. */
  flowId: number | null;
  name: string;
  isSubFlow: boolean;

  /** New on the branch, changed on it, or not on it - hidden. */
  change: GitChangeKindEnum;
  isDone: boolean;
}
