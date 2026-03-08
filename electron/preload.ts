const { contextBridge, ipcRenderer } = require("electron");

interface RequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Optional ID to match requests with responses
}

const api = {
  // Send message to backend → returns Promise with response
  // (uses invoke → main must reply via event)
  invoke: <T = unknown>(msg: RequestMessage): Promise<T> =>
    ipcRenderer.invoke("send-to-backend", msg) as Promise<T>,

  // Fire-and-forget style (no response expected)
  send: (msg: RequestMessage): void => {
    ipcRenderer.send("send-to-backend-fire-and-forget", msg);
  },

  // Listen for messages coming FROM backend
  // Returns unsubscribe function
  onMessage: <T = unknown>(callback: (msg: T) => void): (() => void) => {
    const listener = (_: any, msg: any) => {
      callback(msg as T);
    };

    ipcRenderer.on("recieve-from-backend", listener);

    return () => {
      ipcRenderer.removeListener("recieve-from-backend", listener);
    };
  },

  // One-time listener
  // onceMessage: <T = unknown>(callback: (msg: T) => void): void => {
  //   ipcRenderer.once("recieve-from-backend", (_, msg) => callback(msg as T));
  // },
};

// Expose only what we want
contextBridge.exposeInMainWorld("backendApi", api);

// Type declaration so TS knows about it in renderer
// actually it makes this "window.backendApi;" not error.
// export {};
// declare global {
//   interface Window {
//     backendApi: typeof api;
//   }
// }
