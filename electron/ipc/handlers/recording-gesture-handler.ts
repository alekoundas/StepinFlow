/**
 * Recording Gesture IPC Handler
 *
 * The window a gesture asks in, while the recording is paused at it:
 *  - openWindow:   frameless, always on top, never in a screenshot, over the display the click was
 *                  on. Unlike the bar it takes the keyboard, since its forms are typed into.
 *  - resizeWindow: the page sizes the window to what it shows.
 *  - closeWindow:  closes it. The backend gives the keyboard back to the window that had it.
 *
 * It belongs to this process, so nothing done on it is recorded, and the recording is paused anyway.
 */

import { BrowserWindow, ipcMain, screen } from "electron";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { loadPage, PRELOAD_PATH } from "../../app-paths.js";

const WIDTH = 640;
const INITIAL_HEIGHT = 520;

let gestureWindow: BrowserWindow | null = null;

export function registerRecordingGestureHandler(mainWindow: BrowserWindow | null): void {
  ipcMain.handle(
    IPC_CHANNELS.RECORDING_GESTURE_OPEN_WINDOW,
    async (_event, pressIndex: number, gesture: string): Promise<void> => {
      // A new question replaces one still open: there is only ever one.
      closeRecordingGestureWindow();

      const window = createElectronWindow();
      gestureWindow = window;

      const closeWithMain = () => {
        if (!window.isDestroyed()) window.destroy();
      };
      mainWindow?.once("closed", closeWithMain);

      window.once("closed", () => {
        if (gestureWindow === window) gestureWindow = null;
        mainWindow?.removeListener("closed", closeWithMain);
      });

      const query = new URLSearchParams({ pressIndex: String(pressIndex), gesture });

      try {
        await loadPage(window, `/recording-gesture?${query.toString()}`);
      } catch (error) {
        console.error("[RecordingGestureHandler]: Failed to open the window:", error);
        if (!window.isDestroyed()) window.destroy();
      }
    },
  );

  ipcMain.handle(IPC_CHANNELS.RECORDING_GESTURE_CLOSE_WINDOW, (): void => {
    closeRecordingGestureWindow();
  });

  // Keeps its top, so it grows downwards as a form opens.
  ipcMain.on(IPC_CHANNELS.RECORDING_GESTURE_RESIZE_WINDOW, (event, height: number): void => {
    if (!gestureWindow || gestureWindow.isDestroyed()) return;
    if (event.sender.id !== gestureWindow.webContents.id) return;

    const bounds = gestureWindow.getBounds();
    const workArea = screen.getDisplayMatching(bounds).workArea;
    const newHeight = Math.min(Math.ceil(height), workArea.height);
    const y = Math.min(bounds.y, workArea.y + workArea.height - newHeight);

    gestureWindow.setBounds({ x: bounds.x, y, width: bounds.width, height: newHeight });
  });
}

/** Closes the question's window when the recording no longer asks, or the bar goes. */
export function closeRecordingGestureWindow(): void {
  if (gestureWindow && !gestureWindow.isDestroyed()) gestureWindow.destroy();
  gestureWindow = null;
}

//=====================================================================
// Create and open window
//=====================================================================
function createElectronWindow(): BrowserWindow {
  // The cursor is still where the gesture was pressed.
  const cursor = screen.getCursorScreenPoint();
  const workArea = screen.getDisplayNearestPoint(cursor).workArea;

  const window = new BrowserWindow({
    x: Math.round(workArea.x + (workArea.width - WIDTH) / 2),
    y: Math.round(workArea.y + Math.max(48, (workArea.height - INITIAL_HEIGHT) / 3)),
    width: WIDTH,
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
    hasShadow: false,
    title: "What is this for?",
    backgroundColor: "#00000000",
    webPreferences: {
      preload: PRELOAD_PATH,
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
    },
  });

  window.setAlwaysOnTop(true, "screen-saver");
  window.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });

  // Left out of every screenshot, the recorder's own included. Not available on Linux.
  window.setContentProtection(true);

  window.once("ready-to-show", () => {
    window.show();
    window.focus();
  });

  return window;
}
