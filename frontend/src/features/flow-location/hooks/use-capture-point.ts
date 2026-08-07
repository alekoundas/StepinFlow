import { useCallback, useEffect, useRef, useState } from "react";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { ScreenPointDto } from "@/shared/models/screen-point.dto";
import type {
  IpcBroadcastMessage,
  RecordedInput,
} from "../../../../../electron/shared/types";

interface Props {
  capturePoint: () => Promise<ScreenPointDto | null>;
  cancelCapture: () => void;
  isCapturing: boolean;
}

/**
 * Arms "click anywhere to pick a point".
 *
 * No capture window is opened: the global hook is already running, so the backend just starts
 * broadcasting input and the next click anywhere on screen resolves the promise with its physical
 * coordinates. Escape cancels.
 *
 * The click is not swallowed, so it also reaches whatever is underneath the cursor. That is what
 * makes it possible to pick a point inside a live application.
 */
export function useCapturePoint(): Props {
  const [isCapturing, setIsCapturing] = useState(false);

  // Set while a capture is in flight so unmount and cancel can tear the same session down.
  const teardownRef = useRef<(() => void) | null>(null);

  const capturePoint = useCallback((): Promise<ScreenPointDto | null> => {
    if (teardownRef.current) return Promise.resolve(null);

    setIsCapturing(true);

    return new Promise<ScreenPointDto | null>((resolve) => {
      let unsubscribe: (() => void) | null = null;

      const finish = (point: ScreenPointDto | null) => {
        if (!teardownRef.current) return;
        teardownRef.current = null;

        unsubscribe?.();
        backendApiService.System.inputRecordPointCaptureStop().catch(() => {});
        setIsCapturing(false);
        resolve(point);
      };

      teardownRef.current = () => finish(null);

      unsubscribe = backendApiService.OnBroadcast(
        (event: IpcBroadcastMessage<RecordedInput>) => {
          if (event.type !== "POINT_CAPTURE_EVENT") return;

          // BUTTON_DOWN, not BUTTON_UP: the press that armed this capture happened before
          // recording started, so its release is the only stale event that can arrive.
          if (event.payload.type === "BUTTON_DOWN") {
            finish({
              x: event.payload.physicalX,
              y: event.payload.physicalY,
            });
            return;
          }

          if (
            event.payload.type === "KEY_UP" &&
            event.payload.keyCode === "Escape"
          ) {
            finish(null);
          }
        },
      );

      backendApiService.System.inputRecordPointCaptureStart().catch(() =>
        finish(null),
      );
    });
  }, []);

  const cancelCapture = useCallback(() => {
    teardownRef.current?.();
  }, []);

  // Never leave the backend recording because the dialog was closed mid capture.
  useEffect(() => () => teardownRef.current?.(), []);

  return { capturePoint, cancelCapture, isCapturing };
}
