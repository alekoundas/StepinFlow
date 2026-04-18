import { ScreenshotRequestDto } from "@/shared/models/lazy-data/screenshot-request.dto";
import { ElectronApiService } from "@/shared/services/electron-api-service";
import { useState, useCallback } from "react";
import type { AreaRect } from "../../../../../electron/shared/types";

// interface AreaRect {
//   x: number;
//   y: number;
//   width: number;
//   height: number;
// }

interface Props {
  openWindow: () => Promise<AreaRect | null>;
  isWindowOpen: boolean;
}

export function useWindowOverlay(): Props {
  const [isWindowOpen, setIsWindowOpen] = useState(false);

  const openWindow = useCallback(async (): Promise<AreaRect | null> => {
    setIsWindowOpen(true);
    try {
      const screenshotRequestDto = new ScreenshotRequestDto({
        isVirtualScreen: true,
      });
      const rect =
        await ElectronApiService.searchArea.openWindow(screenshotRequestDto);
      return rect;
    } catch (err) {
      console.error("[useSearchAreaCapture]: capture failed:", err);
      return null;
    } finally {
      setIsWindowOpen(false);
    }
  }, []);

  return { openWindow, isWindowOpen };
}
