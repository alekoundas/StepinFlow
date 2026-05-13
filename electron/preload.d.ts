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
        openWindow: (imageData: Uint8Array) => Promise<Uint8Array | null>;
        signalReady: () => Promise<Uint8Array>;
        signalCloseWindow: (imageData: Uint8Array | null) => void;
      };
    };
  }
}

export {};
