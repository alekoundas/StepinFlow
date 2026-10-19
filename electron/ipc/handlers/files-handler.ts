/**
 * Files IPC Handler
 *
 * The system's own file dialogs and file manager, which only the main process can open.
 *  - chooseFolder: where an export goes, starting in the folder given.
 *  - chooseScript: a .sflw file to import, starting in the folder given.
 *  - showInFolder: the file manager, with the file selected.
 * A dialog the user cancels answers null.
 */

import { BrowserWindow, dialog, ipcMain, shell } from "electron";
import type { OpenDialogOptions } from "electron";
import { IPC_CHANNELS } from "../../shared/channels.js";

export function registerFilesHandler(mainWindow: BrowserWindow | null): void {
  // Over the main window when there is one, so the dialog is its child rather than lost behind it.
  const choose = async (options: OpenDialogOptions): Promise<string | null> => {
    let result;
    if (mainWindow) result = await dialog.showOpenDialog(mainWindow, options);
    else result = await dialog.showOpenDialog(options);

    if (result.canceled || result.filePaths.length === 0) return null;
    return result.filePaths[0];
  };

  ipcMain.handle(IPC_CHANNELS.FILES_CHOOSE_FOLDER, (_event, defaultPath: string | null): Promise<string | null> =>
    choose({
      title: "Export to",
      defaultPath: defaultPath ?? undefined,
      properties: ["openDirectory", "createDirectory"],
    }),
  );

  ipcMain.handle(IPC_CHANNELS.FILES_CHOOSE_SCRIPT, (_event, defaultPath: string | null): Promise<string | null> =>
    choose({
      title: "Import a flow",
      defaultPath: defaultPath ?? undefined,
      properties: ["openFile"],
      filters: [{ name: "Flow scripts", extensions: ["sflw"] }],
    }),
  );

  ipcMain.handle(IPC_CHANNELS.FILES_SHOW_IN_FOLDER, (_event, path: string): void => {
    shell.showItemInFolder(path);
  });
}
