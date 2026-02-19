const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("backendApi", {
  send: (msg: object) => ipcRenderer.invoke("send-to-backend", msg),
  onMessage: (callback: (msg: object) => void) =>
    ipcRenderer.on("recieve-from-backend", (_: any, msg: any) => callback(msg)),
});
