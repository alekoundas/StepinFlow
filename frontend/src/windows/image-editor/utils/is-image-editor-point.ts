import type { ImageEditorResult, ImageEditorPoint } from "../../../../../electron/shared/types";

export const isImageEditorPoint = (
  result: ImageEditorResult,
): result is ImageEditorPoint =>
  result !== null && typeof result === "object" && "x" in result;
