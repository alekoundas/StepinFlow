import { backendApiService } from "@/shared/services/backend-api-service";
import type { Rectangle } from "electron";
import type {
  ImageEditorOpenRequest,
  ImageEditorResult,
} from "../../../../electron/shared/types";

// window.electronApi is declared once in shared/services/electron-api.service.ts, against the same
// ElectronApi interface the preload implements. This stays a thin pass-through.
export const ElectronApiService = {
  backendApi: backendApiService,

  overlay: {
    openCaptureWindow: (
      parentSearchAreaBounds?: Rectangle | null,
      parentAreaName?: string | null,
    ) =>
      window.electronApi.overlay.openCaptureWindow(
        parentSearchAreaBounds,
        parentAreaName,
      ),
    signalReady: () => window.electronApi.overlay.signalReady(),
    signalCloseWindow: (rect: Rectangle | null) =>
      window.electronApi.overlay.signalCloseWindow(rect),
  },

  imageEditor: {
    openWindow: (request: ImageEditorOpenRequest): Promise<ImageEditorResult> =>
      window.electronApi.imageEditor.openWindow(request),
    signalReady: () => window.electronApi.imageEditor.signalReady(),
    signalCloseWindow: (result: ImageEditorResult) =>
      window.electronApi.imageEditor.signalCloseWindow(result),
  },
};
