/**
 * Electron Process IPC Handler
 *
 * What the renderer cannot know about the Electron process by itself.
 *  - getId: this process owns every StepinFlow window, so a recording can leave them out.
 *    The renderer is a process of its own, so its id would match none of them.
 */

import { ipcMain } from "electron";
import { IPC_CHANNELS } from "../../shared/channels.js";

export function registerElectronProcessHandler(): void {
  ipcMain.handle(IPC_CHANNELS.ELECTRON_PROCESS_GET_ID, (): number => process.pid);
}
