import { BrowserWindow, Display, ipcMain, Rectangle, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { InvokeBackend } from "./backend-request-handler.js";
import {
  ScreenshotMonitorResponseDto,
  SignalReadyResponse,
} from "../../shared/types.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Make sure user cant open second window.
let isWindowOpen = false;

interface MonitorEntry {
  screenshotMonitorResponse: ScreenshotMonitorResponseDto; // physical bounds + screenshot bytes
  dipBounds: Rectangle; // same monitor in DIPs, which is what BrowserWindow wants
  display: Display; // Electron display, gives us scaleFactor
  electronWindow: BrowserWindow | null; // Electron ovelay window
}

export async function registerOverlayCaptureHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
  invokeBackend: InvokeBackend,
): Promise<void> {
  ipcMain.handle(
    IPC_CHANNELS.OVERLAY_OPEN_CAPTURE_WINDOW,
    async (
      _event,
      parentSearchAreaBounds: Rectangle | null,
      parentAreaName: string | null,
    ): Promise<Rectangle | null> => {
      if (isWindowOpen) {
        console.warn("[OverlayHandler]: Overlay already open");
        return null;
      }
      isWindowOpen = true;
      let monitorEntries: MonitorEntry[] = [];

      try {
        // 1. Get screenshots from .Net (physical bounds + physical px screenshots)
        const responses = await getScreenshot(invokeBackend);
        if (!responses || responses.length === 0) {
          console.error("[OverlayHandler]: Cant get the screenshot!");
          return null;
        }

        // 2. Physical bounds -> DIPs, then find the display that DIP rect sits on
        monitorEntries = toMonitorEntries(responses);

        // 3. Create new window per monitor.
        for (const monitorEntry of monitorEntries) {
          const newWindow = createElectronWindow(isDev, monitorEntry.dipBounds);
          monitorEntry.electronWindow = newWindow;
        }

        // 4. Ask .Net to start broadcasting mouse click and drag.
        //    Every event the overlay reacts to arrives this way, Escape included, so failing
        //    here and carrying on leaves a fullscreen window the user cannot get out of.
        const started = await invokeBackend("System.inputRecordOverlayStart", null);
        if (!started?.isSuccess) {
          console.error(
            "[OverlayHandler]: Could not start input recording:",
            started?.errorMessage,
          );
          closeWindows(monitorEntries);
          return null;
        }

        // 5. Register per-window ready signal handler BEFORE loading pages
        registerSignalReadyHandlers(
          monitorEntries,
          parentSearchAreaBounds ?? null,
          parentAreaName ?? null,
        );

        // 6. Navigate to overlay page on every window.
        await Promise.all(
          monitorEntries.map((x) => {
            if (!x.electronWindow) return; // will never happen

            if (isDev) {
              x.electronWindow.loadURL(
                "http://localhost:5173/#/overlay-capture",
              );
              x.electronWindow.webContents.openDevTools();
            } else {
              x.electronWindow.loadFile(
                path.join(__dirname, "../dist/frontend/index.html"),
                { hash: "/overlay-capture" },
              );
            }
          }),
        );

        // 7. Wait for result (any window can send it — first one wins)
        return await registerSignalCloseHandler(monitorEntries, invokeBackend);
      } catch (err) {
        // Without this an exception after step 3 leaves fullscreen always-on-top windows with
        // no close handler attached, which is unrecoverable for the user.
        console.error("[OverlayHandler]: Failed to open overlay:", err);
        ipcMain.removeHandler(IPC_CHANNELS.OVERLAY_SIGNAL_READY);
        await invokeBackend("System.inputRecordOverlayStop", null);
        closeWindows(monitorEntries);
        return null;
      } finally {
        isWindowOpen = false;
      }
    },
  );
}

//=====================================================================
// Call .Net to get the screenshot byte[]
//=====================================================================
async function getScreenshot(
  invokeBackend: InvokeBackend,
): Promise<ScreenshotMonitorResponseDto[]> {
  try {
    const result = await invokeBackend("System.captureForOverlay", null);

    return (
      (result as { success: boolean; data: ScreenshotMonitorResponseDto[] })
        .data ?? []
    );
  } catch (err) {
    console.error("[OverlayHandler] Screenshot failed:", err);
    return [];
  }
}

//=====================================================================
// Create and open window
//=====================================================================
function createElectronWindow(isDev: boolean, dipBounds: Rectangle): BrowserWindow {
  // Create window
  const newWindow = new BrowserWindow({
    x: dipBounds.x,
    y: dipBounds.y,
    width: dipBounds.width,
    height: dipBounds.height,
    fullscreen: true,
    frame: false,
    transparent: true,
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    movable: false,
    focusable: false,
    hasShadow: false,
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

  newWindow.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });
  newWindow.setAlwaysOnTop(true, "screen-saver"); // highest possible level
  newWindow.setIgnoreMouseEvents(false);

  return newWindow;
}

//=====================================================================
// Listen for signals from react.
// 'SignalReady' => page loaded and return image from .Net
// 'SignalCloseWindow' => operation completed - return user selection to main electron window
//=====================================================================
function closeWindows(monitorEntries: MonitorEntry[]): void {
  for (const entry of monitorEntries) {
    if (entry.electronWindow && !entry.electronWindow.isDestroyed())
      entry.electronWindow.destroy();
  }
}

function registerSignalReadyHandlers(
  monitorEntries: MonitorEntry[],
  parentSearchAreaBounds: Rectangle | null,
  parentAreaName: string | null,
): void {
  // Clear any handler left behind by a session that ended abnormally: ipcMain.handle throws on
  // a duplicate, and that throw would happen after the windows are already on screen.
  ipcMain.removeHandler(IPC_CHANNELS.OVERLAY_SIGNAL_READY);

  ipcMain.handle(
    IPC_CHANNELS.OVERLAY_SIGNAL_READY,
    async (event): Promise<SignalReadyResponse | null> => {
      // Find which window sent this
      const senderId = event.sender.id;
      const monitorEntry = monitorEntries.find(
        (x) => x.electronWindow?.webContents.id === senderId,
      );

      if (monitorEntry) {
        return {
          screenshot: monitorEntry.screenshotMonitorResponse.screenshot,
          physicalWidth: monitorEntry.screenshotMonitorResponse.width,
          physicalHeight: monitorEntry.screenshotMonitorResponse.height,
          logicalWidth: monitorEntry.dipBounds.width,
          logicalHeight: monitorEntry.dipBounds.height,
          scaleFactor: monitorEntry.display.scaleFactor,
          monitorLogicalOrigin: {
            x: monitorEntry.dipBounds.x,
            y: monitorEntry.dipBounds.y,
          },
          // Physical absolute, so every monitor's overlay gets the same rect and clips it itself.
          parentSearchAreaBounds,
          parentAreaName,
        };
      }
      return null;
    },
  );
}

function registerSignalCloseHandler(
  monitorEntries: MonitorEntry[],
  invokeBackend: InvokeBackend,
): Promise<Rectangle | null> {
  return new Promise<Rectangle | null>((resolve) => {
    const electronWindows = monitorEntries
      .map((x) => x.electronWindow)
      .filter((x) => x !== null);

    // Runs once. Closing the windows fires their own "closed" listeners, which call back in here.
    let isCleanedUp = false;

    // Resolves only after the recorder has actually stopped, so reopening the overlay cannot
    // race the stop and find the recorder still busy.
    const cleanup = async () => {
      if (isCleanedUp) return;
      isCleanedUp = true;

      ipcMain.removeHandler(IPC_CHANNELS.OVERLAY_SIGNAL_READY); //remove the READY handler if the user cancelled before signalReady fired
      electronWindows.forEach((window) => {
        if (!window.isDestroyed()) window.close();
      });

      try {
        await invokeBackend("System.inputRecordOverlayStop", null);
      } catch (err) {
        console.error("[OverlayHandler]: Could not stop input recording:", err);
      }
    };

    ipcMain.once(
      IPC_CHANNELS.OVERLAY_SIGNAL_CLOSE_WINDOW,
      (_event, rect: Rectangle | null) => {
        void cleanup().then(() => resolve(rect));
      },
    );

    // If user force-closes the overlay window (e.g. Alt+F4)
    electronWindows.forEach((win) => {
      win.once("closed", () => {
        void cleanup().then(() => resolve(null));
      });
    });
  });
}

//=====================================================================
// Monitor Matching
// The backend is Per-Monitor-V2 aware, so its bounds are physical pixels. screenToDipRect is
// the supported conversion into the DIP space BrowserWindow positions live in.
//=====================================================================

function toMonitorEntries(
  responses: ScreenshotMonitorResponseDto[],
): MonitorEntry[] {
  return responses.map((response) => {
    const dipBounds = screen.screenToDipRect(null, {
      x: response.x,
      y: response.y,
      width: response.width,
      height: response.height,
    });

    const display: Display = screen.getDisplayNearestPoint({
      x: dipBounds.x,
      y: dipBounds.y,
    });

    return {
      screenshotMonitorResponse: response,
      dipBounds,
      display,
      electronWindow: null,
    };
  });
}
