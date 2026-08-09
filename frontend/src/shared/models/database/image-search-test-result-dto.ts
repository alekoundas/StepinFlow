export interface ImageSearchTestImageDto {
  flowStepImageId: number;
  name: string;
  isFound: boolean;
  isRequired: boolean;
  matchCount: number;
  bestScore: number;
  bestX: number;
  bestY: number;
  scale: number;
}

export interface ImageSearchTestResultDto {
  isResolved: boolean;
  errorMessage?: string;

  searchAreaX: number;
  searchAreaY: number;
  searchAreaWidth: number;
  searchAreaHeight: number;

  totalMatches: number;
  wouldSucceed: boolean;

  images: ImageSearchTestImageDto[];
}
