import type { Point } from "@/windows/image-editor/types/point";

/**
 * View transform (zoom + pan), between the two coordinate spaces used across the editor:
 *  - IMAGE space   : integer pixels of the image being edited (0,0 = top-left pixel)
 *  - VIEWPORT space: CSS pixels relative to the element hosting the canvases
 *
 *  viewport = image * scale + offset
 *  image    = (viewport - offset) / scale
 */
export interface ViewTransform {
  scale: number;
  offset: Point;
}
