/** Pixel grid rendering options. */
export interface GridOptions {
  enabled: boolean;
  opacity: number;
  /** Only drawn once one image pixel covers at least this many screen px. */
  minScale: number;
}
