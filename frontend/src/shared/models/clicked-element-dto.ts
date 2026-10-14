import type { ElementFoundByEnum } from "@/shared/enums/backend/element-found-by-enum";

/**
 * What a click hit: the element's rectangle inside the main area, and the text on it. The
 * rectangle is what the draft cuts a template from, and the text is what names the step.
 */
export interface ClickedElementDto {
  /** The click or drag it belongs to, by its index among the recording's actions. */
  actionIndex: number;

  // In the main area's pixels, from its top left.
  x: number;
  y: number;
  width: number;
  height: number;

  /** Read by OCR. Empty when there is none, or none could be read. */
  text: string;

  foundBy: ElementFoundByEnum;

  /** Why it is a box around the click rather than what the model found. */
  note?: string | null;

  /** The element as a PNG, base64: what a template cut from it would be. */
  image: string;
}
