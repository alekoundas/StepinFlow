import type { RepositoryBranchSwitchFlowDto } from "@/shared/models/repository/repository-branch-switch-flow-dto";

/** A branch switch as it goes: sent whole on every change, and returned once it is done. */
export interface RepositoryBranchSwitchDto {
  branch: string;

  /** The flows hidden first, then the ones imported, sub-flows before their callers. */
  flows: RepositoryBranchSwitchFlowDto[];

  /** Where a pull kept the commits it dropped, when it dropped any. */
  backupBranch: string | null;
}
