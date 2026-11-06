/**
 * Auto-updater IPC Handler
 *
 *  - getStatus: what the auto-updater is doing. A page asks as it loads, then is told of each change.
 *  - check: looks for a new version now.
 *  - restart: closes StepinFlow, installs the downloaded version and opens it again.
 */

import { ipcMain } from "electron";
import { IPC_CHANNELS } from "../../shared/channels.js";
import type { AutoUpdaterStatus } from "../../shared/types.js";
import {
  checkForNewVersion,
  getAutoUpdaterStatus,
  restartToInstall,
} from "../services/auto-updater-service.js";

export function registerAutoUpdaterHandler(): void {
  ipcMain.handle(IPC_CHANNELS.AUTO_UPDATER_STATUS_GET, (): AutoUpdaterStatus => getAutoUpdaterStatus());

  ipcMain.handle(IPC_CHANNELS.AUTO_UPDATER_CHECK, (): void => checkForNewVersion());

  ipcMain.handle(IPC_CHANNELS.AUTO_UPDATER_RESTART, (): void => restartToInstall());
}
