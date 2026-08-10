import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { Rectangle } from "electron";
import { useState, useCallback } from "react";

interface Props {
  openWindow: (
    parentSearchAreaBounds?: Rectangle | null,
    parentSearchAreaName?: string | null,
  ) => Promise<Rectangle | null>;
  isWindowOpen: boolean;
}

export function useWindowOverlay(): Props {
  const [isWindowOpen, setIsWindowOpen] = useState(false);

  const openWindow = useCallback(
    async (
      parentSearchAreaBounds?: Rectangle | null,
      parentSearchAreaName?: string | null,
    ): Promise<Rectangle | null> => {
      setIsWindowOpen(true);
      try {
        const rect = await ElectronApiService.overlay.openCaptureWindow(
          parentSearchAreaBounds,
          parentSearchAreaName,
        );
        return rect;
      } catch (err) {
        console.error("[useWindowOverlay]: capture failed:", err);
        return null;
      } finally {
        setIsWindowOpen(false);
      }
    },
    [],
  );

  return { openWindow, isWindowOpen };
}
