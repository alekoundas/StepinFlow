export interface FlowSearchAreaPreviewDto {
  isResolved: boolean;
  errorMessage?: string;

  locationX: number;
  locationY: number;
  width: number;
  height: number;

  // JPEG bytes, arrives as base64.
  screenshot?: string;
}
