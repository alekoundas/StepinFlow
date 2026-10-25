import type { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";

/** A flow with changes not committed yet. */
export interface RepositoryFlowStatusDto {
  flowId: number;
  flowName: string;
  changeCount: number;
}

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

/** A file not committed yet, as the commit dialog lists it. */
export interface RepositoryFileChangeDto {
  path: string;
  kind: GitChangeKindEnum;

  /** Null once the flow is gone, when only its files are left to commit. */
  flowId: number | null;
  flowName: string;
  isImage: boolean;

  /** Base64: a template image as it is now, or as it was when it is the one deleted. */
  image: string | null;
}

/** A template image that changed, before and after, each in base64. */
export interface RepositoryImageChangeDto {
  name: string;
  kind: GitChangeKindEnum;
  before: string | null;
  after: string | null;
}

/** One flow's files against the last commit. */
export interface RepositoryFlowChangesDto {
  flowId: number;
  scriptPath: string;
  changeCount: number;
  script: string;

  /** Null when the last commit has no such file. */
  committedScript: string | null;
  images: RepositoryImageChangeDto[];
}

export interface RepositoryCommitRequestDto {
  message: string;
  paths: string[];
}

export interface RepositoryCommitDto {
  sha: string;
  shortSha: string;
  message: string;
  authorName: string;
  when: string;
}

export interface RepositoryFlowCommitRequestDto {
  flowId: number;
  sha: string;
}

/** What one commit changed in one flow. */
export interface RepositoryFlowCommitDto {
  commit: RepositoryCommitDto;
  scriptBefore: string | null;
  scriptAfter: string | null;
  images: RepositoryImageChangeDto[];
}

export interface RepositoryChangedDto {
  flowIds: number[];
}

export interface RepositoryBranchDto {
  name: string;
  isCurrent: boolean;
  lastCommitAt: string;

  /** Only on the remote: switching to it makes it here. */
  isRemoteOnly: boolean;
}

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

/** A branch switch as it goes: sent whole on every change, and returned once it is done. */
export interface RepositoryBranchSwitchDto {
  branch: string;

  /** The flows hidden first, then the ones imported, sub-flows before their callers. */
  flows: RepositoryBranchSwitchFlowDto[];

  /** Where a pull kept the commits it dropped, when it dropped any. */
  backupBranch: string | null;
}

export interface RepositoryConnectRequestDto {
  url: string;

  /** Empty keeps the token already saved. */
  token: string;
}

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

/** What connecting found: an empty remote it sent the branches to, or one with history. */
export interface RepositoryConnectDto {
  remoteUrl: string;
  pushedBranches: string[];

  /** What pulling the branch checked out would do, when the remote has that branch. */
  pull: RepositoryPullDto | null;
}

export interface RepositoryPushDto {
  branch: string;
  commitCount: number;
}
