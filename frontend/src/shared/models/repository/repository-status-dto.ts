import type { RepositoryFlowStatusDto } from "@/shared/models/repository/repository-flow-status-dto";

/** Where the repository is, the branch, and which flows have changes not committed yet. */
export interface RepositoryStatusDto {
  folderPath: string;
  branch: string;
  commitCount: number;
  lastCommitAt: string | null;

  /** Every changed file, whichever flow it belongs to. */
  changeCount: number;

  /** Only the flows with changes. */
  flows: RepositoryFlowStatusDto[];

  /** What the user's git config says, shown where the Settings fields are left empty. */
  configuredAuthorName: string;
  configuredAuthorEmail: string;

  /** Null when there is no remote. */
  remoteUrl: string | null;

  /** Whether the remote had the branch when it was last fetched. */
  isBranchOnRemote: boolean;

  /** Commits on the branch the remote does not have, and the other way round, as of the last fetch. */
  aheadCount: number;
  behindCount: number;
}
