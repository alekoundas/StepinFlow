import type { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";

/** A template image that changed, before and after, each in base64. */
export interface RepositoryImageChangeDto {
  name: string;
  kind: GitChangeKindEnum;
  before: string | null;
  after: string | null;
}
