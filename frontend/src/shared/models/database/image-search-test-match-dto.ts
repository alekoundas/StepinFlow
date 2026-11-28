import type { MatchRefusalEnum } from "@/shared/enums/backend/match-refusal-enum";

// One hit, in coordinates relative to the search area. The screenshot is that same area, so
// these need no conversion before being drawn on it.
export interface ImageSearchTestMatchDto {
  // False for the closest candidate under the accuracy, and for a shape its colour or contrast refused.
  isAccepted: boolean;

  x: number;
  y: number;
  width: number;
  height: number;

  score: number;
  scale: number;

  // Where the cursor would actually land: the template's click offset, scaled.
  clickX: number;
  clickY: number;

  // Shape and colour only.
  colour?: number | null;
  colourLimit?: number | null;
  contrast?: number | null;
  refusedBy?: MatchRefusalEnum | null;
}
