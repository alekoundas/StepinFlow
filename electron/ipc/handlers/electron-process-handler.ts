/**
 * Electron Process IPC Handler
 *
 * What the renderer cannot know about, or do to, the Electron process by itself.
 *  - getId: this process owns every StepinFlow window, so a recording can leave them out.
 *    The renderer is a process of its own, so its id would match none of them.
 *  - getStatus: whether the backend this process connects to can answer yet.
 *  - relaunch: closes StepinFlow and opens it again, which starts a new backend.
 */

import { app, ipcMain } from "electron";
import { IPC_CHANNELS } from "../../shared/channels.js";
import type { BackendStatus } from "../../shared/types.js";
import { getBackendStatus } from "../services/backend-status-service.js";

export function registerElectronProcessHandler(): void {
  ipcMain.handle(IPC_CHANNELS.ELECTRON_PROCESS_GET_ID, (): number => process.pid);

  ipcMain.handle(IPC_CHANNELS.BACKEND_STATUS_GET, (): BackendStatus => getBackendStatus());

  ipcMain.handle(IPC_CHANNELS.ELECTRON_PROCESS_RELAUNCH, (): void => {
    // A portable build and an AppImage unpack to a folder that goes when this process does, so
    // those are started from the file the user opened.
    app.relaunch({ execPath: process.env.PORTABLE_EXECUTABLE_FILE ?? process.env.APPIMAGE });
    app.exit(0);
  });
}
