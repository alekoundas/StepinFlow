import { BrowserWindow, desktopCapturer, ipcMain, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export interface AreaRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export function registerImageEditorHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): void {
  ipcMain.handle(
    IPC_CHANNELS.IMAGE_EDITOR_WINDOW_OPEN,
    async (_, initialDataUrl: string, stepId?: string) => {
      const editorWin = new BrowserWindow({
        width: 1200,
        height: 800,
        frame: false,
        transparent: false, // we use canvas for transparency
        alwaysOnTop: true,
        webPreferences: {
          preload: path.join(
            __dirname,
            isDev ? "preload.js" : "../dist/preload.js",
          ),
          nodeIntegration: false,
          contextIsolation: true,
          sandbox: true,
        },
      });

      if (isDev) {
        await editorWin.loadURL("http://localhost:5173/template-editor");
      } else {
        await editorWin.loadFile(
          path.join(__dirname, "../dist/frontend/index.html"),
          { hash: "/template-editor" },
        );
      }

      // Send initial screenshot
      editorWin.webContents.once("did-finish-load", () => {
        editorWin.webContents.send(IPC_CHANNELS.IMAGE_EDITOR_WINDOW_READY, {
          dataUrl: initialDataUrl,
          stepId,
        });
      });

      return new Promise<{ pngBytes: Uint8Array; stepId?: string }>(
        (resolve) => {
          ipcMain.once(
            IPC_CHANNELS.IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW,
            (_, result) => {
              editorWin.close();
              resolve(result);
            },
          );

          editorWin.once("closed", () =>
            resolve({ pngBytes: new Uint8Array(0) }),
          );
        },
      );
    },
  );
}
