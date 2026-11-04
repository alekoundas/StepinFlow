import { BrowserWindow } from "electron";
import log from "electron-log";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { BackendStateEnum, type BackendStatus } from "../../shared/types.js";

let status: BackendStatus = {
  state: BackendStateEnum.STARTING,
  hadStarted: false,
  reason: "",
  logPath: "",
};
let isQuitting = false;

export function getBackendStatus(): BackendStatus {
  return status;
}

// The pipes open once the migrations are done, so a connection means the database is ready too.
export function setBackendReady(): void {
  if (status.state !== BackendStateEnum.STARTING) return;

  change({ ...status, state: BackendStateEnum.READY });
}

export function setBackendStopped(reason: string): void {
  if (status.state === BackendStateEnum.STOPPED) return;

  change({
    state: BackendStateEnum.STOPPED,
    hadStarted: status.state === BackendStateEnum.READY,
    reason,
    logPath: log.transports.file.getFile().path,
  });
}

// Quitting stops the backend on purpose, and no page should flash an error about it on the way out.
export function setBackendQuitting(): void {
  isQuitting = true;
}

function change(next: BackendStatus): void {
  status = next;
  if (isQuitting) return;

  BrowserWindow.getAllWindows().forEach((window) => {
    if (!window.isDestroyed()) window.webContents.send(IPC_CHANNELS.BACKEND_STATUS_CHANGED, status);
  });
}
