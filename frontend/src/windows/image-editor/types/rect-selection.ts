import type { Rect } from "@/windows/image-editor/types/rect";

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
