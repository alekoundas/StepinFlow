import type { RepositoryImageChangeDto } from "@/shared/models/repository/repository-image-change-dto";

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
