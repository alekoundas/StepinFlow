import type { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";

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
