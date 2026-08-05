import {
  SignalReadyResponse,
  IpcRequestMessage,
  IpcBroadcastMessage,
} from "./shared/types.js";

declare global {
  interface Window {
    electronApi: {
      backendApi: {
        invoke: <T = unknown>(msg: IpcRequestMessage) => Promise<T>;
        onBroadcast: <T = unknown>(
          callback: (msg: IpcBroadcastMessage<T>) => void,
        ) => () => void;
      };
      overlay: {
        openCaptureWindow: () => Promise<Electron.Rectangle | null>;
        openPreviewWindow: () => Promise<null>;
        signalReady: () => Promise<SignalReadyResponse | null>;
        signalCloseWindow: (rect: Electron.Rectangle | null) => void;
      };
      imageEditor: {
        openWindow: (imageBase64: string) => Promise<string | null>;
        signalReady: () => Promise<string | null>;
        signalCloseWindow: (imageBase64: string | null) => void;
      };
    };
  }
}

export {};
