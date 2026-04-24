const { contextBridge, ipcRenderer } = require("electron");

// ========== Types & Interfaces ==========
interface RequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Optional ID to match requests with responses
}

interface SignalReadyResponse {
  screenshot: Uint8Array;
  physicalWidth: number;
  physicalHeight: number;
  logicalWidth: number;
  logicalHeight: number;
  scaleFactor: number;
}

interface SignalMouseEvent {
  type: "down" | "move" | "up";
  physicalX: number;
  physicalY: number;
}

const IPC_CHANNELS = {
  // ========== Backend pipe channels =================
  BACKEND_SEND: "BACKEND_SEND",
  BACKEND_RECEIVE: "BACKEND_RECEIVE",
  BACKEND_DISCONNECTED: "BACKEND_DISCONNECTED",

  // ========== Search-area overlay channels ==========
  SEARCH_AREA_OPEN_WINDOW: "SEARCH_AREA_OPEN_WINDOW",
  SEARCH_AREA_BROADCAST_MOUSE_EVENT: "SEARCH_AREA_BROADCAST_MOUSE_EVENT",
  SEARCH_AREA_SIGNAL_READY: "SEARCH_AREA_SIGNAL_READY",
  SEARCH_AREA_SIGNAL_MOUSE_EVENT: "SEARCH_AREA_SIGNAL_MOUSE_EVENT",
  SEARCH_AREA_SIGNAL_CLOSE_WINDOW: "SEARCH_AREA_SIGNAL_CLOSE_WINDOW",

  // ========== Image editor channels ==========
  IMAGE_EDITOR_WINDOW_OPEN: "IMAGE_EDITOR_WINDOW_OPEN",
  IMAGE_EDITOR_WINDOW_READY: "IMAGE_EDITOR_WINDOW_READY",
  IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW: "IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW",
} as const;

const api = {
  backendApi: {
    // Send message to backend → returns Promise with response
    invoke: <T = unknown>(msg: RequestMessage): Promise<T> =>
      ipcRenderer.invoke(IPC_CHANNELS.BACKEND_SEND, msg) as Promise<T>,

    // Listen for messages coming FROM backend. Returns unsubscribe function
    onMessage: <T = unknown>(callback: (msg: T) => void): (() => void) => {
      const listener = (_: any, msg: any) => {
        callback(msg as T);
      };

      ipcRenderer.on(IPC_CHANNELS.BACKEND_RECEIVE, listener);

      return () => {
        ipcRenderer.removeListener(IPC_CHANNELS.BACKEND_RECEIVE, listener);
      };
    },
  },

  searchArea: {
    openWindow: (): Promise<Electron.Rectangle | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.SEARCH_AREA_OPEN_WINDOW),
     broadcastMouseEvent: (callback: (event: any) => void): (() => void) => {
      const listener = (_: any, event: any) => callback(event);
      ipcRenderer.on(IPC_CHANNELS.SEARCH_AREA_BROADCAST_MOUSE_EVENT, listener);
      return () =>
        ipcRenderer.removeListener(IPC_CHANNELS.SEARCH_AREA_BROADCAST_MOUSE_EVENT, listener);
    },
    signalReady: (): Promise<SignalReadyResponse | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.SEARCH_AREA_SIGNAL_READY),
    signalMouseEvent: (event: SignalMouseEvent) =>
      ipcRenderer.invoke(IPC_CHANNELS.SEARCH_AREA_SIGNAL_MOUSE_EVENT),
    signalCloseWindow: (rect: Electron.Rectangle | null): void =>
      ipcRenderer.send(IPC_CHANNELS.SEARCH_AREA_SIGNAL_CLOSE_WINDOW, rect),
  },

  imageEditor: {
    // openWindow: (screenshotRequest: any): Promise<Electron.Rectangle | null> =>
    //   ipcRenderer.invoke(
    //     IPC_CHANNELS.SEARCH_AREA_WINDOW_OPEN,
    //     screenshotRequest,
    //   ),
    // signalReady: (): Promise<SignalReadyResponse> =>
    //   ipcRenderer.invoke(IPC_CHANNELS.SEARCH_AREA_WINDOW_READY),
    // signalCloseWindow: (rect: Electron.Rectangle | null): void =>
    //   ipcRenderer.send(IPC_CHANNELS.SEARCH_AREA_RETURN_RESULT_TO_WINDOW, rect),
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
