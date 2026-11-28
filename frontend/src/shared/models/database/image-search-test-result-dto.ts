import type { ImageSearchTestImageDto } from "@/shared/models/database/image-search-test-image-dto";

export interface ImageSearchTestResultDto {
  isResolved: boolean;
  errorMessage?: string;

  searchAreaX: number;
  searchAreaY: number;
  searchAreaWidth: number;
  searchAreaHeight: number;

  // The exact pixels the matches were found in. JPEG, arrives as base64.
  screenshot?: string | null;

  totalMatches: number;
  wouldSucceed: boolean;

  // The contrast Shape and colour allows, the same for every template.
  contrastMin: number;
  contrastMax: number;

  images: ImageSearchTestImageDto[];
}
