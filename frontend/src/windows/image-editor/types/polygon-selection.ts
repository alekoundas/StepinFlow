import type { Point } from "@/windows/image-editor/types/point";

export interface PolygonSelection {
  kind: "polygon";
  points: Point[];
  committed: boolean;
}
