import type { ImageSearchTestMatchDto } from "@/shared/models/database/image-search-test-match-dto";

export interface ImageSearchTestImageDto {
  flowStepTemplateId: number;
  name: string;
  isFound: boolean;
  matchCount: number;
  bestScore: number;
  bestX: number;
  bestY: number;
  scale: number;
  matches: ImageSearchTestMatchDto[];
}
