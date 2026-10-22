import { ElectronApiService } from "@/shared/services/electron-api-service";
import type { Rectangle } from "electron";
import { useState, useCallback } from "react";
import type { OverlayCaptureResult } from "../../../../../electron/shared/types";

interface Props {
  openWindow: (
    parentSearchAreaBounds?: Rectangle | null,
    parentAreaName?: string | null,
    withImage?: boolean,
  ) => Promise<OverlayCaptureResult | null>;
  isWindowOpen: boolean;
}

export function useWindowOverlay(): Props {
  const [isWindowOpen, setIsWindowOpen] = useState(false);

  const openWindow = useCallback(
    async (
      parentSearchAreaBounds?: Rectangle | null,
      parentAreaName?: string | null,
      withImage?: boolean,
    ): Promise<OverlayCaptureResult | null> => {
      setIsWindowOpen(true);
      try {
        return await ElectronApiService.overlay.openCaptureWindow(
          parentSearchAreaBounds,
          parentAreaName,
          withImage,
        );
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
