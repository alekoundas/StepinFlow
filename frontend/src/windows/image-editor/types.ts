/**
 * Image Editor Type Definitions
 *
 * Coordinate spaces used across the editor:
 *  - IMAGE space   : integer pixels of the image being edited (0,0 = top-left pixel)
 *  - VIEWPORT space: CSS pixels relative to the element hosting the canvases
 *
 *  viewport = image * scale + offset
 *  image    = (viewport - offset) / scale
 */

export interface Point {
  x: number;
  y: number;
}

export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface Size {
  width: number;
  height: number;
}

/** Tools available in the left rail. */
export type EditorTool =
  | "hand"
  | "crop-rect"
  | "crop-lasso"
  | "crop-polygon"
  | "eraser";

/**
 * A selection drawn on the image but not applied yet.
 * `committed` flips to true when the drag/polygon finishes, which is when the
 * confirm bar appears.
 */
export interface RectSelection {
  kind: "rect";
  rect: Rect;
  committed: boolean;
}

export interface PolygonSelection {
  kind: "polygon";
  points: Point[];
  committed: boolean;
}

export type PendingSelection = RectSelection | PolygonSelection;

/** View transform (zoom + pan). */
export interface ViewTransform {
  scale: number;
  offset: Point;
}

/** One undo/redo step. `canvas` holds the full pixel snapshot. */
export interface HistoryEntry {
  id: string;
  label: string;
  canvas: HTMLCanvasElement;
  thumbnail: string;
  timestamp: number;
}

/** Pixel grid rendering options. */
export interface GridOptions {
  enabled: boolean;
  opacity: number;
  /** Only drawn once one image pixel covers at least this many screen px. */
  minScale: number;
}
