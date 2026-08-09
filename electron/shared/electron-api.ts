import type {
  ImageEditorOpenRequest,
  ImageEditorReadyResponse,
  ImageEditorResult,
  IpcBroadcastMessage,
  IpcRequestMessage,
  ResultDto,
  SignalReadyResponse,
} from "./types.js";

/**
 * The surface contextBridge exposes on window.electronApi.
 *
 * One definition, implemented by preload and consumed by the renderer, so the two cannot drift.
 * Types only, so importing this from a sandboxed preload costs nothing at runtime.
 */
export interface ElectronApi {
  backendApi: {
    invoke: <T = unknown>(msg: IpcRequestMessage) => Promise<ResultDto<T>>;
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

  // Images travel as base64 PNG strings (what .Net returns for byte[])
  imageEditor: {
    openWindow: (request: ImageEditorOpenRequest) => Promise<ImageEditorResult>;
    signalReady: () => Promise<ImageEditorReadyResponse | null>;
    signalCloseWindow: (result: ImageEditorResult) => void;
  };
}
