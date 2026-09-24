export interface FlowAreaPreviewDto {
  isResolved: boolean;
  errorMessage?: string;

  locationX: number;
  locationY: number;
  width: number;
  height: number;

  // The DPI of the monitor holding most of it: what a capture inside it records.
  dpi: number;

  // JPEG bytes, arrives as base64.
  screenshot?: string;
}
