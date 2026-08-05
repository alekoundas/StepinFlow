/**
 * Image Editor IPC Handler
 *
 * Manages:
 *  - Opening the image editor window
 *  - Handing the source image to the React page
 *  - Receiving the edited PNG back and returning it to the caller
 *
 * Communication flow:
 *  1. Renderer calls openWindow(pngBase64)
 *  2. Handler creates the editor window and loads the image editor page
 *  3. React page calls signalReady() and gets the image back
 *  4. User edits, then saves or cancels
 *  5. React calls signalCloseWindow(pngBase64 | null)
 *  6. Handler closes the window and resolves the original openWindow() call
 *
 * Images travel as base64 PNG strings: that is what .Net already produces for
 * byte[] over the JSON payload, so no conversion is needed on either end.
 */

import { BrowserWindow, ipcMain } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Make sure the user cant open a second editor window.
let isWindowOpen = false;

export async function registerImageEditorHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): Promise<void> {
  ipcMain.handle(
    IPC_CHANNELS.EDITOR_OPEN_WINDOW,
    async (_event, imageBase64: string): Promise<string | null> => {
      if (isWindowOpen) {
        console.warn("[ImageEditorHandler]: Editor already open");
        return null;
      }

      if (!imageBase64) {
        console.error("[ImageEditorHandler]: No image passed to the editor");
        return null;
      }

      isWindowOpen = true;

      try {
        // 1. Create the editor window.
        const editorWindow = createElectronWindow(isDev, mainWindow);

        // 2. Register the ready signal BEFORE loading the page, otherwise the
        //    page can call it before the handler exists.
        registerSignalReadyHandler(editorWindow, imageBase64);

        // 3. Navigate to the image editor page.
        if (isDev) {
          await editorWindow.loadURL("http://localhost:5173/#/image-editor");
          editorWindow.webContents.openDevTools({ mode: "detach" });
        } else {
          await editorWindow.loadFile(
            path.join(__dirname, "../dist/frontend/index.html"),
            { hash: "/image-editor" },
          );
        }

        // 4. Wait for the user to save or cancel.
        return await registerSignalCloseHandler(editorWindow);
      } catch (error) {
        console.error("[ImageEditorHandler]: Failed to open editor:", error);
        ipcMain.removeHandler(IPC_CHANNELS.EDITOR_SIGNAL_READY);
        return null;
      } finally {
        isWindowOpen = false;
      }
    },
  );
}

//=====================================================================
// Create and open window
//=====================================================================
function createElectronWindow(
  isDev: boolean,
  parent: BrowserWindow | null,
): BrowserWindow {
  const editorWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 900,
    minHeight: 600,
    show: false, // avoid a white flash while the page loads
    frame: true,
    title: "Edit template image",
    backgroundColor: "#14161c",
    parent: parent ?? undefined,
    modal: false,
    webPreferences: {
      preload: path.join(
        __dirname,
        isDev ? "../../preload.js" : "../../dist/preload.js",
      ),
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
    },
  });

  editorWindow.once("ready-to-show", () => {
    editorWindow.maximize();
    editorWindow.show();
    editorWindow.focus();
  });

  return editorWindow;
}

//=====================================================================
// Listen for signals from react.
// 'SignalReady'       => page loaded, hand it the image to edit
// 'SignalCloseWindow' => user saved (base64 PNG) or cancelled (null)
//=====================================================================
function registerSignalReadyHandler(
  electronWindow: BrowserWindow,
  imageBase64: string,
): void {
  // Clear any handler left behind by a previous (crashed) session.
  ipcMain.removeHandler(IPC_CHANNELS.EDITOR_SIGNAL_READY);

  ipcMain.handle(
    IPC_CHANNELS.EDITOR_SIGNAL_READY,
    async (event): Promise<string | null> => {
      if (event.sender.id !== electronWindow.webContents.id) return null;
      return imageBase64;
    },
  );
}

function registerSignalCloseHandler(
  electronWindow: BrowserWindow,
): Promise<string | null> {
  return new Promise<string | null>((resolve) => {
    const onCloseSignal = (
      event: Electron.IpcMainEvent,
      imageBase64: string | null,
    ) => {
      if (event.sender.id !== electronWindow.webContents.id) return;
      cleanup();
      resolve(imageBase64);
    };

    const onClosed = () => {
      cleanup();
      resolve(null);
    };

    const cleanup = () => {
      // Remove the READY handler so the next open can register a fresh one.
      ipcMain.removeHandler(IPC_CHANNELS.EDITOR_SIGNAL_READY);
      ipcMain.removeListener(
        IPC_CHANNELS.EDITOR_SIGNAL_CLOSE_WINDOW,
        onCloseSignal,
      );
      electronWindow.removeListener("closed", onClosed);

      if (!electronWindow.isDestroyed()) electronWindow.close();
    };

    ipcMain.on(IPC_CHANNELS.EDITOR_SIGNAL_CLOSE_WINDOW, onCloseSignal);

    // If the user force-closes the editor window (e.g. Alt+F4)
    electronWindow.once("closed", onClosed);
  });
}
