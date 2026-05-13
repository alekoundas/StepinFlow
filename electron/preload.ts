const { contextBridge, ipcRenderer } = require("electron");
// ========== Types & Interfaces ==========
interface IpcRequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Optional ID to match requests with responses
}

interface IpcBroadcastMessage<T> {
  type: string;
  payload: T;
}

interface SignalReadyResponse {
  screenshot: Uint8Array;
  physicalWidth: number;
  physicalHeight: number;
  logicalWidth: number;
  logicalHeight: number;
  scaleFactor: number;
  monitorLogicalOrigin: { x: number; y: number };
}

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

const api = {
  backendApi: {
    // Send message to backend → returns Promise with response
    invoke: <T = unknown>(msg: IpcRequestMessage): Promise<T> =>
      ipcRenderer.invoke(IPC_CHANNELS.BACKEND_SEND, msg) as Promise<T>,

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
    openCaptureWindow: (): Promise<Electron.Rectangle | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.OVERLAY_OPEN_CAPTURE_WINDOW),
    openPreviewWindow: (): Promise<null> =>
      ipcRenderer.invoke(IPC_CHANNELS.OVERLAY_OPEN_PREVIEW_WINDOW),
    signalReady: (): Promise<SignalReadyResponse | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.OVERLAY_SIGNAL_READY),
    signalCloseWindow: (rect: Electron.Rectangle | null): void =>
      ipcRenderer.send(IPC_CHANNELS.OVERLAY_SIGNAL_CLOSE_WINDOW, rect),
  },

  imageEditor: {
    openWindow: (imageData: Uint8Array): Promise<Uint8Array | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.EDITOR_OPEN_WINDOW, imageData),
    signalReady: (): Promise<Uint8Array> =>
      ipcRenderer.invoke(IPC_CHANNELS.EDITOR_SIGNAL_READY),
    signalCloseWindow: (imageData: Uint8Array | null): void =>
      ipcRenderer.send(IPC_CHANNELS.EDITOR_SIGNAL_CLOSE_WINDOW, imageData),
  },
};

// Expose only what we want
contextBridge.exposeInMainWorld("electronApi", api);

// Type declaration so TS knows about it in renderer
// actually it makes this "window.electronApi;" not error.
// export {};
// declare global {
//   interface Window {
//     electronApi: typeof api;
//   }
// }
