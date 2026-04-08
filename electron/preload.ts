const { contextBridge, ipcRenderer } = require("electron");

// ========== Types & Interfaces ==========
interface RequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Optional ID to match requests with responses
}

interface AreaRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

const IPC_CHANNELS = {
  BACKEND_SEND: "BACKEND_SEND",
  BACKEND_RECEIVE: "BACKEND_RECEIVE",
  BACKEND_DISCONNECTED: "BACKEND_DISCONNECTED",

  SEARCH_AREA_OPEN: "SEARCH_AREA_OPEN",
  SEARCH_AREA_RESULT: "SEARCH_AREA_RESULT",
  SEARCH_AREA_READY: "SEARCH_AREA_READY",
  SEARCH_AREA_SCREENSHOT: "SEARCH_AREA_SCREENSHOT",
} as const;
//  ===========================================

const api = {
  backendApi: {
    // Send message to backend → returns Promise with response
    invoke: <T = unknown>(msg: RequestMessage): Promise<T> =>
      ipcRenderer.invoke(IPC_CHANNELS.BACKEND_SEND, msg) as Promise<T>,

    // Fire-and-forget style (no response expected)
    // send: (msg: RequestMessage): void => {
    //   ipcRenderer.send(IPC_CHANNELS.BACKEND_SEND, msg);
    // },

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
    // One-time listener
    // onceMessage: <T = unknown>(callback: (msg: T) => void): void => {
    //   ipcRenderer.once(IPC_CHANNELS.BACKEND_RECEIVE, (_, msg) => callback(msg as T));
    // },
  },

  searchArea: {
    capture: (): Promise<AreaRect | null> =>
      ipcRenderer.invoke(
        IPC_CHANNELS.SEARCH_AREA_OPEN,
      ) as Promise<AreaRect | null>,
  },

  sendResult: (rect: AreaRect | null): void => {
    ipcRenderer.send(IPC_CHANNELS.SEARCH_AREA_RESULT, rect);
  },

  onScreenshot: (callback: (dataUrl: string) => void): (() => void) => {
    const listener = (_: Electron.IpcRendererEvent, dataUrl: string) =>
      callback(dataUrl);
    ipcRenderer.on(IPC_CHANNELS.SEARCH_AREA_SCREENSHOT, listener);
    return () =>
      ipcRenderer.removeListener(IPC_CHANNELS.SEARCH_AREA_SCREENSHOT, listener);
  },

  signalReady: (): void => {
    ipcRenderer.send(IPC_CHANNELS.SEARCH_AREA_READY);
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
