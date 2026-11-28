import { BackendApiService } from "@/shared/services/backend-api-service";
import type { Rectangle } from "electron";
import type {
  ImageEditorOpenRequest,
  ImageEditorResult,
  AutoUpdaterStatus,
} from "../../../../electron/shared/types";

// window.electronApi is declared once in shared/services/electron-api.service.ts, against the same
// ElectronApi interface the preload implements. This stays a thin pass-through.
export const ElectronApiService = {
  BackendApi: BackendApiService,

  ElectronProcess: {
    relaunch: () => window.electronApi.electronProcess.relaunch(),
  },

  AutoUpdater: {
    getStatus: () => window.electronApi.autoUpdater.getStatus(),
    onStatusChange: (callback: (status: AutoUpdaterStatus) => void) =>
      window.electronApi.autoUpdater.onStatusChange(callback),
    check: () => window.electronApi.autoUpdater.check(),
    restart: () => window.electronApi.autoUpdater.restart(),
  },

  Overlay: {
    openCaptureWindow: (
      parentSearchAreaBounds?: Rectangle | null,
      parentAreaName?: string | null,
      withImage?: boolean,
    ) =>
      window.electronApi.overlay.openCaptureWindow(
        parentSearchAreaBounds,
        parentAreaName,
        withImage,
      ),
    signalReady: () => window.electronApi.overlay.signalReady(),
    signalCloseWindow: (rect: Rectangle | null) =>
      window.electronApi.overlay.signalCloseWindow(rect),
  },

  ImageEditor: {
    openWindow: (request: ImageEditorOpenRequest): Promise<ImageEditorResult> =>
      window.electronApi.imageEditor.openWindow(request),
    signalReady: () => window.electronApi.imageEditor.signalReady(),
    signalCloseWindow: (result: ImageEditorResult) =>
      window.electronApi.imageEditor.signalCloseWindow(result),
  },

  RecordingBar: {
    openWindow: (flowId: number, setupFlowId?: number) =>
      window.electronApi.recordingBar.openWindow(flowId, setupFlowId ?? null),
    closeWindow: () => window.electronApi.recordingBar.closeWindow(),
    resizeWindow: (width: number, height: number) =>
      window.electronApi.recordingBar.resizeWindow(width, height),
  },

  RecordingGesture: {
    openWindow: (pressIndex: number, gesture: string) =>
      window.electronApi.recordingGesture.openWindow(pressIndex, gesture),
    closeWindow: () => window.electronApi.recordingGesture.closeWindow(),
    resizeWindow: (height: number) => window.electronApi.recordingGesture.resizeWindow(height),
  },

  // A cancelled dialog answers null.
  Files: {
    chooseFolder: (defaultPath?: string) => window.electronApi.files.chooseFolder(defaultPath ?? null),
    chooseScript: (defaultPath?: string) => window.electronApi.files.chooseScript(defaultPath ?? null),
    showInFolder: (path: string) => window.electronApi.files.showInFolder(path),
    pathOf: (file: File) => window.electronApi.files.pathOf(file),
  },
};
