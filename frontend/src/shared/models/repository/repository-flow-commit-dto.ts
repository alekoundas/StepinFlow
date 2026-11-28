import type { RepositoryCommitDto } from "@/shared/models/repository/repository-commit-dto";
import type { RepositoryImageChangeDto } from "@/shared/models/repository/repository-image-change-dto";

/** What one commit changed in one flow. */
export interface RepositoryFlowCommitDto {
  commit: RepositoryCommitDto;
  scriptBefore: string | null;
  scriptAfter: string | null;
  images: RepositoryImageChangeDto[];
}
