export interface RepositoryBranchDto {
  name: string;
  isCurrent: boolean;
  lastCommitAt: string;

  /** Only on the remote: switching to it makes it here. */
  isRemoteOnly: boolean;
}
