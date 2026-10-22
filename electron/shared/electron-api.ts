import type {
  ImageEditorOpenRequest,
  ImageEditorReadyResponse,
  ImageEditorResult,
  IpcBroadcastMessage,
  IpcRequestMessage,
  OverlayCaptureResult,
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

  // Electron's main process, which owns every StepinFlow window.
  electronProcess: {
    getId: () => Promise<number>;
  };

  overlay: {
    // Physical-pixel bounds the selection must stay inside, and the name to show for them.
    openCaptureWindow: (
      parentSearchAreaBounds?: Electron.Rectangle | null,
      parentAreaName?: string | null,
      withImage?: boolean,
    ) => Promise<OverlayCaptureResult | null>;
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

  // The floating bar a recording is controlled from.
  recordingBar: {
    // The flow to record against, and the flow the setup created, which a discard from the bar
    // deletes with the recording.
    openWindow: (flowId: number, setupFlowId?: number | null) => Promise<void>;
    closeWindow: () => Promise<void>;
    // What the page shows, in CSS pixels.
    resizeWindow: (width: number, height: number) => void;
  };

  // The system's file dialogs and file manager. A cancelled dialog answers null.
  files: {
    chooseFolder: (defaultPath?: string | null) => Promise<string | null>;
    chooseScript: (defaultPath?: string | null) => Promise<string | null>;
    showInFolder: (path: string) => Promise<void>;
    // The path of a file dropped on the page. Empty for one that is not on disk.
    pathOf: (file: File) => string;
  };
}
