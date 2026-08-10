// Types come from the shared contract via import() type expressions rather than import
// statements. An import statement would make this a module, and tsc would then emit "export {}"
// into the output, which a sandboxed CommonJS preload cannot load.
//
// IPC_CHANNELS below is still inlined because it is a runtime value, and a sandboxed preload
// cannot require its own modules. Bundling the preload is what removes that last duplication.
type ElectronApi = import("./shared/electron-api.js").ElectronApi;
type IpcRequestMessage = import("./shared/types.js").IpcRequestMessage;
type IpcBroadcastMessage<T> =
  import("./shared/types.js").IpcBroadcastMessage<T>;
type ResultDto<T> = import("./shared/types.js").ResultDto<T>;
type SignalReadyResponse = import("./shared/types.js").SignalReadyResponse;
type ImageEditorOpenRequest =
  import("./shared/types.js").ImageEditorOpenRequest;
type ImageEditorReadyResponse =
  import("./shared/types.js").ImageEditorReadyResponse;
type ImageEditorResult = import("./shared/types.js").ImageEditorResult;

const { contextBridge, ipcRenderer } = require("electron");

const IPC_CHANNELS = {
  // ========== Backend pipe channels =================
  BACKEND_SEND: "BACKEND_SEND",
  BACKEND_RECEIVE: "BACKEND_RECEIVE",
  BACKEND_BROADCAST: "BACKEND_BROADCAST",
  BACKEND_DISCONNECTED: "BACKEND_DISCONNECTED",

  // ========== Overlay (Search-area)  channels ==========
  OVERLAY_OPEN_CAPTURE_WINDOW: "OVERLAY_OPEN_CAPTURE_WINDOW",
  OVERLAY_OPEN_PREVIEW_WINDOW: "OVERLAY_OPEN_PREVIEW_WINDOW",
  OVERLAY_SIGNAL_READY: "OVERLAY_SIGNAL_READY",
  OVERLAY_SIGNAL_CLOSE_WINDOW: "OVERLAY_SIGNAL_CLOSE_WINDOW",

  // ========== Image editor channels ==========
  EDITOR_OPEN_WINDOW: "EDITOR_OPEN_WINDOW",
  EDITOR_SIGNAL_READY: "EDITOR_SIGNAL_READY",
  EDITOR_SIGNAL_CLOSE_WINDOW: "EDITOR_SIGNAL_CLOSE_WINDOW",
} as const;

// Typed against the contract so the renderer and this file cannot drift.
const api: ElectronApi = {
  backendApi: {
    // Send message to backend → returns Promise with response
    invoke: <T = unknown>(msg: IpcRequestMessage): Promise<ResultDto<T>> =>
      ipcRenderer.invoke(IPC_CHANNELS.BACKEND_SEND, msg) as Promise<
        ResultDto<T>
      >,

    // Listen for messages coming FROM backend. Returns unsubscribe function
    onBroadcast: <T = unknown>(
      callback: (msg: IpcBroadcastMessage<T>) => void,
    ): (() => void) => {
      const listener = (_: any, msg: any) => {
        callback(msg);
      };

      ipcRenderer.on(IPC_CHANNELS.BACKEND_BROADCAST, listener);

      return () => {
        ipcRenderer.removeListener(IPC_CHANNELS.BACKEND_BROADCAST, listener);
      };
    },
  },

  overlay: {
    openCaptureWindow: (
      parentSearchAreaBounds?: Electron.Rectangle | null,
      parentSearchAreaName?: string | null,
    ): Promise<Electron.Rectangle | null> =>
      ipcRenderer.invoke(
        IPC_CHANNELS.OVERLAY_OPEN_CAPTURE_WINDOW,
        parentSearchAreaBounds ?? null,
        parentSearchAreaName ?? null,
      ),
    openPreviewWindow: (): Promise<null> =>
      ipcRenderer.invoke(IPC_CHANNELS.OVERLAY_OPEN_PREVIEW_WINDOW),
    signalReady: (): Promise<SignalReadyResponse | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.OVERLAY_SIGNAL_READY),
    signalCloseWindow: (rect: Electron.Rectangle | null): void =>
      ipcRenderer.send(IPC_CHANNELS.OVERLAY_SIGNAL_CLOSE_WINDOW, rect),
  },

  // Images are passed as base64 PNG strings (same shape .Net returns for byte[])
  imageEditor: {
    openWindow: (request: ImageEditorOpenRequest): Promise<ImageEditorResult> =>
      ipcRenderer.invoke(IPC_CHANNELS.EDITOR_OPEN_WINDOW, request),
    signalReady: (): Promise<ImageEditorReadyResponse | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.EDITOR_SIGNAL_READY),
    signalCloseWindow: (result: ImageEditorResult): void =>
      ipcRenderer.send(IPC_CHANNELS.EDITOR_SIGNAL_CLOSE_WINDOW, result),
  },
};

// Expose only what we want. The renderer's Window augmentation lives in
// frontend/src/shared/services/electron-api.service.ts and points at the same ElectronApi.
contextBridge.exposeInMainWorld("electronApi", api);
