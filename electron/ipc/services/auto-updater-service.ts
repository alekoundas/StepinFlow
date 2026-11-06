import { app, BrowserWindow } from "electron";
import log from "electron-log";
import pkg from "electron-updater";
import { IS_DEVELOPING } from "../../app-paths.js";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { AutoUpdaterStateEnum, type AutoUpdaterStatus } from "../../shared/types.js";

const { autoUpdater } = pkg;

const CHECK_EVERY_MS = 4 * 60 * 60 * 1000;

let status: AutoUpdaterStatus = {
  state: AutoUpdaterStateEnum.NOT_CHECKED,
  currentVersion: app.getVersion(),
  isAskedCheck: false,
  version: "",
  transferredBytes: 0,
  totalBytes: 0,
  logPath: "",
};

// Started from source there is no installer to update, and Electron's own version would show.
if (IS_DEVELOPING) status = { ...status, state: AutoUpdaterStateEnum.UNAVAILABLE, currentVersion: "dev" };

let stateBeforeCheck = status.state;

export function getAutoUpdaterStatus(): AutoUpdaterStatus {
  return status;
}

// Every few hours as well as at start, since the app is left open all day for a flow.
export function startAutoUpdaterChecks(): void {
  if (IS_DEVELOPING) return;

  autoUpdater.logger = log;
  autoUpdater.disableWebInstaller = true;

  autoUpdater.on("update-not-available", () => change({ ...status, state: AutoUpdaterStateEnum.UP_TO_DATE }));
  autoUpdater.on("update-available", (info) =>
    change({
      ...status,
      state: AutoUpdaterStateEnum.DOWNLOADING,
      version: info.version,
      transferredBytes: 0,
      totalBytes: 0,
    }),
  );
  autoUpdater.on("download-progress", (progress) =>
    change({ ...status, transferredBytes: progress.transferred, totalBytes: progress.total }),
  );
  autoUpdater.on("update-downloaded", (info) =>
    change({ ...status, state: AutoUpdaterStateEnum.READY, version: info.version }),
  );
  autoUpdater.on("error", onAutoUpdaterError);

  check(false);
  setInterval(() => check(false), CHECK_EVERY_MS);
}

export function checkForNewVersion(): void {
  check(true);
}

export function restartToInstall(): void {
  if (status.state !== AutoUpdaterStateEnum.READY) return;

  // Silent, and the installer opens the app again when it is done.
  autoUpdater.quitAndInstall(true, true);
}

function check(isAsked: boolean): void {
  if (status.state === AutoUpdaterStateEnum.UNAVAILABLE) return;
  // Checking again would announce the same update and start its progress over.
  if (status.state === AutoUpdaterStateEnum.DOWNLOADING || status.state === AutoUpdaterStateEnum.READY)
    return;

  if (status.state === AutoUpdaterStateEnum.CHECKING) {
    if (isAsked) change({ ...status, isAskedCheck: true });
    return;
  }

  stateBeforeCheck = status.state;
  change({ ...status, state: AutoUpdaterStateEnum.CHECKING, isAskedCheck: isAsked });

  // A failed download rejects here too. Its error event is what reports both.
  autoUpdater
    .checkForUpdates()
    .then((result) => result?.downloadPromise)
    .catch(() => {});
}

function onAutoUpdaterError(): void {
  // A check nobody asked for fails whenever the machine is offline, which is not worth interrupting for.
  if (status.state === AutoUpdaterStateEnum.CHECKING && !status.isAskedCheck) {
    change({ ...status, state: stateBeforeCheck });
    return;
  }

  change({ ...status, state: AutoUpdaterStateEnum.FAILED, logPath: log.transports.file.getFile().path });
}

function change(next: AutoUpdaterStatus): void {
  status = next;

  BrowserWindow.getAllWindows().forEach((window) => {
    if (!window.isDestroyed()) window.webContents.send(IPC_CHANNELS.AUTO_UPDATER_STATUS_CHANGED, status);
  });
}
