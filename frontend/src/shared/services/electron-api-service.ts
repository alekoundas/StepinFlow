import type { ResultDto } from "@/shared/models/result-dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { Rectangle } from "electron";
import type {
  SignalMouseEvent,
  SignalReadyResponse,
} from "../../../../electron/shared/types";

// TODO remove this. Buut Build process throws error without it....
// const backendApi = window.backendApi; // old way
// declare const electronApi: {
//   backendApi: {
//     invoke: <T>(msg: any) => Promise<ResultDto<T>>;
//     onMessage: <T>(cb: (msg: T) => void) => () => void;
//   };
//   searchArea: {
//     capture: () => Promise<Electron.Rectangle | null>;
//     sendResult: (rect: Electron.Rectangle | null) => void;
//     onScreenshot: (callback: (dataUrl: string) => void) => () => void;
//     signalReady: () => void;
//   };
// };

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = any>(msg: any) => Promise<ResultDto<T>>;
        onBroadcast: <T>(callback: (msg: T) => void) => () => void;
      };
      searchArea: {
        openWindow: () => Promise<Electron.Rectangle | null>;
        broadcastMouseEvent: (
          callback: (e: SignalMouseEvent) => void,
        ) => () => void;
        signalReady: () => Promise<SignalReadyResponse | null>;
        signalCloseWindow: (rect: Electron.Rectangle | null) => void;
      };
    };
  }
}

export const ElectronApiService = {
  backendApi: backendApiService,
  searchArea: {
    openWindow: () => window.electronApi.searchArea.openWindow(),
    broadcastMouseEvent: (callback: (e: SignalMouseEvent) => void) =>
      window.electronApi.searchArea.broadcastMouseEvent(callback),
    signalReady: () => window.electronApi.searchArea.signalReady(),
    signalCloseWindow: (rect: Rectangle | null) =>
      window.electronApi.searchArea.signalCloseWindow(rect),
  },
};
