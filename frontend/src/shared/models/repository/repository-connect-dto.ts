import type { RepositoryPullDto } from "@/shared/models/repository/repository-pull-dto";

/** What connecting found: an empty remote it sent the branches to, or one with history. */
export interface RepositoryConnectDto {
  remoteUrl: string;
  pushedBranches: string[];

  /** What pulling the branch checked out would do, when the remote has that branch. */
  pull: RepositoryPullDto | null;
}
