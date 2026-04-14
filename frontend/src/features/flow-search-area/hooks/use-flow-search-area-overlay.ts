import { ElectronApiService } from "@/shared/services/electron-api-service";
import { useState, useCallback } from "react";

interface AreaRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

interface UseSearchAreaCaptureReturn {
  capture: () => Promise<AreaRect | null>;
  isCapturing: boolean;
}

export function useSearchAreaCapture(): UseSearchAreaCaptureReturn {
  const [isCapturing, setIsCapturing] = useState(false);

  const capture = useCallback(async (): Promise<AreaRect | null> => {
    setIsCapturing(true);
    try {
      const rect = await ElectronApiService.searchArea.openWindow();
      return rect;
    } catch (err) {
      console.error("[useSearchAreaCapture] capture failed:", err);
      return null;
    } finally {
      setIsCapturing(false);
    }
  }, []);

  return { capture, isCapturing };
}
