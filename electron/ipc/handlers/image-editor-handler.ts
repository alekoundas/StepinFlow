/**
 * Image Editor IPC Handler
 *
 * Manages:
 *  - Opening the image editor window
 *  - Passing image data to React component
 *  - Receiving edited PNG bytes back and returning to caller
 *
 * Communication flow:
 *  1. Main window calls openImageEditor(imageData)
 *  2. Handler creates new window and loads ImageEditorPage
 *  3. React page signals ready, handler sends image data
 *  4. User edits and clicks Export
 *  5. React sends PNG bytes back via IPC
 *  6. Window closes and result returned to caller
 */

import { BrowserWindow, ipcMain } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

let editorWindowOpen = false; // Prevent multiple windows

export async function registerImageEditorHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): Promise<void> {
  ipcMain.handle(
    IPC_CHANNELS.EDITOR_OPEN_WINDOW,
    async (_, imageData: Uint8Array): Promise<Uint8Array | null> => {
      // Prevent opening multiple editor windows simultaneously
      if (editorWindowOpen) {
        console.warn("[ImageEditorHandler]: Editor already open");
        return null;
      }

      editorWindowOpen = true;

      try {
        // 1. Create Electron window for image editor
        const editorWin = new BrowserWindow({
          width: 1400,
          height: 900,
          minWidth: 800,
          minHeight: 600,
          frame: true, // Allow system frame for title bar & resize
          transparent: false,
          alwaysOnTop: true,
          icon: undefined, // Use app icon
          webPreferences: {
            preload: path.join(
              __dirname,
              isDev ? "../../preload.js" : "../../dist/preload.js",
            ),
            nodeIntegration: false,
            contextIsolation: true,
            sandbox: true,
            // Enable WebGL for potential future canvas optimizations
            webgl: true,
          },
        });

        // 2. Register signal handler BEFORE loading pages
        registerSignalReadyHandler(imageData);

        // 3. Navigate to image editor page on new window.
        if (isDev) {
          await editorWin.loadURL("http://localhost:5173/#/image-editor");
          editorWin.webContents.openDevTools();
        } else {
          await editorWin.loadFile(
            path.join(__dirname, "../dist/frontend/index.html"),
            { hash: "/image-editor" },
          );
        }

        // 8. Wait for result (any window can send it — first one wins)
        return await registerSignalCloseHandler(editorWin);

        // // 2. Send image data to React once page is loaded
        // return new Promise<Uint8Array | null>((resolve) => {
        //   // Wait for React to signal it's ready to receive image
        //   ipcMain.once(IPC_CHANNELS.IMAGE_EDITOR_WINDOW_READ, () => {
        //     // Send the image data to React
        //     editorWin.webContents.send(IPC_CHANNELS.IMAGE_EDITOR_LOAD_IMAGE, {
        //       dataUrl: initialDataUrl,
        //       stepId,
        //     });
        //   });

        //   // ================================================================
        //   // 3. Listen for export result from React
        //   // ================================================================
        //   ipcMain.once(
        //     IPC_CHANNELS.IMAGE_EDITOR_RETURN_RESULT_TO_WINDOW,
        //     (_, result: { pngBytes: Uint8Array; stepId?: string }) => {
        //       console.log(
        //         "[ImageEditorHandler]: Received edited image, closing window",
        //       );
        //       editorWin.close();
        //       resolve(result);
        //     },
        //   );

        //   // Handle window close without export
        //   editorWin.once("closed", () => {
        //     console.log(
        //       "[ImageEditorHandler]: Editor window closed without export",
        //     );
        //     resolve({ pngBytes: new Uint8Array(0), stepId });
        //   });
        // });
      } finally {
        editorWindowOpen = false;
      }
    },
  );

  //=====================================================================
  // Listen for signals from react.
  // 'SignalReady' => page loaded and return image from React
  // 'SignalCloseWindow' => operation completed - return user selection to main electron window
  //=====================================================================
  function registerSignalReadyHandler(imageData: Uint8Array): void {
    ipcMain.handle(
      IPC_CHANNELS.EDITOR_SIGNAL_READY,
      async (event): Promise<Uint8Array | null> => {
        return imageData;
      },
    );
  }

  function registerSignalCloseHandler(
    electronWindow: BrowserWindow,
  ): Promise<Uint8Array | null> {
    return new Promise<Uint8Array | null>((resolve) => {
      const cleanup = () => {
        ipcMain.removeHandler(IPC_CHANNELS.EDITOR_SIGNAL_READY); //remove the READY handler if the user cancelled before signalReady fired
      };

      ipcMain.once(
        IPC_CHANNELS.EDITOR_SIGNAL_CLOSE_WINDOW,
        (_event, imageData: Uint8Array | null) => {
          cleanup();
          resolve(imageData);
        },
      );

      // If user force-closes the editor window (e.g. Alt+F4)
      electronWindow.once("closed", () => {
        cleanup();
        resolve(null);
      });
    });
  }
}
