import { useCallback, useState } from "react";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import type {
  ImageEditorModeEnum,
  ImageEditorPoint,
  ImageEditorResult,
} from "../../../../../electron/shared/types";

interface Props {
  // EDIT resolves to the edited PNG, PICK_POINT to a point in template pixels.
  openImageEditor: (
    imageBase64: string,
    mode?: ImageEditorModeEnum,
  ) => Promise<ImageEditorResult>;
  isWindowOpen: boolean;
}

export function useWindowImageEditor(): Props {
  const [isWindowOpen, setIsWindowOpen] = useState(false);

  const openImageEditor = useCallback(
    async (
      imageBase64: string,
      mode: ImageEditorModeEnum = "EDIT",
    ): Promise<ImageEditorResult> => {
      setIsWindowOpen(true);
      try {
        return await ElectronApiService.imageEditor.openWindow({
          imageBase64,
          mode,
        });
      } catch (err) {
        console.error("[useWindowImageEditor]: editor failed:", err);
        return null;
      } finally {
        setIsWindowOpen(false);
      }
    },
    [],
  );

  return { openImageEditor, isWindowOpen };
}

export const isImageEditorPoint = (
  result: ImageEditorResult,
): result is ImageEditorPoint =>
  result !== null && typeof result === "object" && "x" in result;
