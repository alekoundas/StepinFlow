import type { RepositoryCommitDto } from "@/shared/models/repository/repository-commit-dto";

/** What a pull of the branch checked out would do, for the person to agree to first. */
export interface RepositoryPullDto {
  branch: string;

  /** The remote's commit the branch would be moved to. */
  remoteSha: string;

  /** Newest first, the commits the remote has that the branch does not. */
  incoming: RepositoryCommitDto[];

  /** Newest first, the commits never pushed. They leave the branch and are kept on a backup branch. */
  dropped: RepositoryCommitDto[];
}
