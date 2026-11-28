/** One undo/redo step. `canvas` holds the full pixel snapshot. */
export interface HistoryEntry {
  id: string;
  label: string;
  canvas: HTMLCanvasElement;
  thumbnail: string;
  timestamp: number;
}
