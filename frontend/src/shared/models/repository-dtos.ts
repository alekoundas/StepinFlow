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
