/**
 * Recording Bar IPC Handler
 *
 * The floating bar a recording is controlled from, while the main window is out of the way:
 *  - openWindow:   frameless, always on top, and never in a screenshot. Minimises the main window.
 *  - resizeWindow: the page sizes the window to what it shows, so no see-through margin sits over
 *                  the application taking its clicks.
 *  - closeWindow:  closes it, and brings the main window back.
 *
 * It never takes the focus, so clicking Pause leaves the keyboard with the application being
 * recorded. It belongs to this process, so nothing done on it is recorded.
 */

import { BrowserWindow, ipcMain, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// The toolbar alone. The page resizes it as soon as it has drawn.
const INITIAL_WIDTH = 640;
const INITIAL_HEIGHT = 56;
const TOP_MARGIN = 16;

let barWindow: BrowserWindow | null = null;

export function registerRecordingBarHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): void {
  ipcMain.handle(
    IPC_CHANNELS.RECORDING_BAR_OPEN_WINDOW,
    async (_event, flowId: number, setupFlowId: number | null): Promise<void> => {
      if (barWindow && !barWindow.isDestroyed()) return;

      const bar = createElectronWindow(isDev);
      barWindow = bar;

      // A bar left behind on its own would keep the app open with nothing to go back to.
      const closeWithMain = () => {
        if (!bar.isDestroyed()) bar.destroy();
      };
      mainWindow?.once("closed", closeWithMain);

      bar.once("closed", () => {
        barWindow = null;
        mainWindow?.removeListener("closed", closeWithMain);
        restore(mainWindow);
      });

      // The flow to record against, and the one the setup created, which goes with a discard made
      // from the bar. A flow recorded into from the editor stays.
      const query = new URLSearchParams({ flowId: String(flowId) });
      if (setupFlowId) query.set("setupFlowId", String(setupFlowId));
      const hash = `/recording-bar?${query.toString()}`;

      try {
        if (isDev) {
          await bar.loadURL(`http://localhost:5173/#${hash}`);
        } else {
          await bar.loadFile(path.join(__dirname, "../dist/frontend/index.html"), {
            hash,
          });
        }
      } catch (error) {
        console.error("[RecordingBarHandler]: Failed to open the bar:", error);
        if (!bar.isDestroyed()) bar.destroy();
        return;
      }

      mainWindow?.minimize();
    },
  );

  ipcMain.handle(IPC_CHANNELS.RECORDING_BAR_CLOSE_WINDOW, (): void => {
    if (barWindow && !barWindow.isDestroyed()) barWindow.destroy();
  });

  // Keeps its centre and its top, wherever the tester dragged it.
  ipcMain.on(
    IPC_CHANNELS.RECORDING_BAR_RESIZE_WINDOW,
    (event, width: number, height: number): void => {
      if (!barWindow || barWindow.isDestroyed()) return;
      if (event.sender.id !== barWindow.webContents.id) return;

      const bounds = barWindow.getBounds();
      const newWidth = Math.ceil(width);
      const newHeight = Math.ceil(height);

      barWindow.setBounds({
        x: Math.round(bounds.x + bounds.width / 2 - newWidth / 2),
        y: bounds.y,
        width: newWidth,
        height: newHeight,
      });
    },
  );
}

//=====================================================================
// Create and open window
//=====================================================================
function createElectronWindow(isDev: boolean): BrowserWindow {
  // Top centre of the screen the tester is on, which is where they clicked Start.
  const cursor = screen.getCursorScreenPoint();
  const workArea = screen.getDisplayNearestPoint(cursor).workArea;

  const bar = new BrowserWindow({
    x: Math.round(workArea.x + (workArea.width - INITIAL_WIDTH) / 2),
    y: workArea.y + TOP_MARGIN,
    width: INITIAL_WIDTH,
    height: INITIAL_HEIGHT,
    show: false,
    frame: false,
    transparent: true,
    resizable: false,
    maximizable: false,
    minimizable: false,
    fullscreenable: false,
    alwaysOnTop: true,
    skipTaskbar: true,
    focusable: false,
    hasShadow: false,
    title: "Recording",
    backgroundColor: "#00000000",
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

  bar.setAlwaysOnTop(true, "screen-saver");
  bar.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });

  // Left out of every screenshot, the recorder's own included. Not available on Linux.
  bar.setContentProtection(true);

  bar.once("ready-to-show", () => bar.showInactive());

  return bar;
}

function restore(mainWindow: BrowserWindow | null): void {
  if (!mainWindow || mainWindow.isDestroyed()) return;

  if (mainWindow.isMinimized()) mainWindow.restore();
  mainWindow.focus();
}
