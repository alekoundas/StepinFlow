import type { ResultDto } from "@/shared/models/result-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { Rectangle } from "electron";
import type {
  IpcBroadcastMessage,
  IpcRequestMessage,
  RecordedInput,
  SignalReadyResponse,
} from "../../../../electron/shared/types";

// TODO remove this.
declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = any>(msg: IpcRequestMessage) => Promise<ResultDto<T>>;
        onBroadcast: <T>(
          callback: (msg: IpcBroadcastMessage<T>) => void,
        ) => () => void;
      };
      overlay: {
        openCaptureWindow: () => Promise<Electron.Rectangle | null>;
        openPreviewWindow: () => Promise<null>;
        broadcastMouseEvent: (
          callback: (e: RecordedInput) => void,
        ) => () => void;
        signalReady: () => Promise<SignalReadyResponse | null>;
        signalCloseWindow: (rect: Electron.Rectangle | null) => void;
      };
    };
  }
}

export const ElectronApiService = {
  backendApi: backendApiService,
  overlay: {
    openCaptureWindow: () => window.electronApi.overlay.openCaptureWindow(),
    broadcastMouseEvent: (callback: (e: RecordedInput) => void) =>
      window.electronApi.overlay.broadcastMouseEvent(callback),
    signalReady: () => window.electronApi.overlay.signalReady(),
    signalCloseWindow: (rect: Rectangle | null) =>
      window.electronApi.overlay.signalCloseWindow(rect),
  },
};
