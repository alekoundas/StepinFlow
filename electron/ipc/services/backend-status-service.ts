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

export function getBackendStatus(): BackendStatus {
  return status;
}

// On its way out counts, since nothing should connect to it any more.
export function isBackendGone(): boolean {
  return status.state === BackendStateEnum.STOPPING || status.state === BackendStateEnum.STOPPED;
}

// The pipes open once the migrations are done, so a connection means the database is ready too.
export function setBackendReady(): void {
  if (status.state !== BackendStateEnum.STARTING) return;

  change({ ...status, state: BackendStateEnum.READY });
}

// The app is closing. The window says so until the backend has left.
export function setBackendStopping(): void {
  change({ ...status, state: BackendStateEnum.STOPPING });
}

export function setBackendStopped(reason: string): void {
  // One that was asked to leave has no error to report.
  if (isBackendGone()) return;

  change({
    state: BackendStateEnum.STOPPED,
    hadStarted: status.state === BackendStateEnum.READY,
    reason,
    logPath: log.transports.file.getFile().path,
  });
}

function change(next: BackendStatus): void {
  status = next;

  BrowserWindow.getAllWindows().forEach((window) => {
    if (!window.isDestroyed()) window.webContents.send(IPC_CHANNELS.BACKEND_STATUS_CHANGED, status);
  });
}
