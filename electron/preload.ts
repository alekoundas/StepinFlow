const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("backendApi", {
  send: (msg: object) => ipcRenderer.invoke("send-to-backend", msg),
  onMessage: (callback: (msg: object) => void) =>
    ipcRenderer.on("backend-message", (_: any, msg: any) => callback(msg)),
});
