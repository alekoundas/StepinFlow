const { contextBridge, ipcRenderer } = require("electron");
// import { IPC_CHANNELS } from "./shared/channels";
// import {
//   RecordedInput,
//   SignalReadyResponse,
//   IpcRequestMessage,
// } from "./shared/types";
// ========== Types & Interfaces ==========
interface IpcRequestMessage {
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
  monitorLogicalOrigin: { x: number; y: number };
}

interface RecordedInput {
  type: RecordedInputTypeEnum;
  physicalX: number;
  physicalY: number;
  cursorButtonType: CursorButtonTypeEnum;
  createdOn: Date;
}

// Enums

const RecordedInputTypeEnum = {
  BUTTON_UP: "BUTTON_UP",
  BUTTON_DOWN: "BUTTON_DOWN",
  CURSOR_DRAG: "CURSOR_DRAG",
  CURSOR_MOVE: "CURSOR_MOVE",
  CURSOR_SCROLL: "CURSOR_SCROLL",
} as const;
type RecordedInputTypeEnum =
  (typeof RecordedInputTypeEnum)[keyof typeof RecordedInputTypeEnum];

const CursorButtonTypeEnum = {
  LEFT_BUTTON: "LEFT_BUTTON",
  RIGHT_BUTTON: "RIGHT_BUTTON",
  MIDDLE_BUTTON: "MIDDLE_BUTTON",
} as const;
type CursorButtonTypeEnum =
  (typeof CursorButtonTypeEnum)[keyof typeof CursorButtonTypeEnum];

const IPC_CHANNELS = {
  // ========== Backend pipe channels =================
  BACKEND_SEND: "BACKEND_SEND",
  BACKEND_RECEIVE: "BACKEND_RECEIVE",
  BACKEND_BROADCAST: "BACKEND_BROADCAST",
  BACKEND_DISCONNECTED: "BACKEND_DISCONNECTED",

  // ========== Search-area overlay channels ==========
  SEARCH_AREA_OPEN_WINDOW: "SEARCH_AREA_OPEN_WINDOW",
  SEARCH_AREA_BROADCAST_MOUSE_EVENT: "SEARCH_AREA_BROADCAST_MOUSE_EVENT",
  SEARCH_AREA_SIGNAL_READY: "SEARCH_AREA_SIGNAL_READY",
  SEARCH_AREA_SIGNAL_CLOSE_WINDOW: "SEARCH_AREA_SIGNAL_CLOSE_WINDOW",

  // ========== Image editor channels ==========
  IMAGE_EDITOR_WINDOW_OPEN: "IMAGE_EDITOR_WINDOW_OPEN",
  IMAGE_EDITOR_WINDOW_READY: "IMAGE_EDITOR_WINDOW_READY",
  IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW: "IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW",
} as const;

const api = {
  backendApi: {
    // Send message to backend → returns Promise with response
    invoke: <T = unknown>(msg: IpcRequestMessage): Promise<T> =>
      ipcRenderer.invoke(IPC_CHANNELS.BACKEND_SEND, msg) as Promise<T>,

    // Listen for messages coming FROM backend. Returns unsubscribe function
    onBroadcast: <T = unknown>(callback: (msg: T) => void): (() => void) => {
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
    broadcastMouseEvent: (
      callback: (data: RecordedInput) => void,
    ): (() => void) => {
      const listener = (_: any, data: RecordedInput) => callback(data);
      ipcRenderer.on(IPC_CHANNELS.SEARCH_AREA_BROADCAST_MOUSE_EVENT, listener);
      return () =>
        ipcRenderer.removeListener(
          IPC_CHANNELS.SEARCH_AREA_BROADCAST_MOUSE_EVENT,
          listener,
        );
    },
    signalReady: (): Promise<SignalReadyResponse | null> =>
      ipcRenderer.invoke(IPC_CHANNELS.SEARCH_AREA_SIGNAL_READY),
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
