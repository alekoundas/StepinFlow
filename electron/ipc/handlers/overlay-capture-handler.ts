import { BrowserWindow, Display, ipcMain, Rectangle, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../../shared/channels.js";
import { InvokeBackend } from "./backend-request-handler.js";
import {
  RecordedInput,
  ScreenshotMonitorResponseDto,
  SignalReadyResponse,
} from "../../shared/types.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Make sure user cant open second window.
let isWindowOpen = false;

interface MonitorEntry {
  screenshotMonitorResponse: ScreenshotMonitorResponseDto; // has logicalX/Y/W/H + screenshotBytes + physicalW/H
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
    async (_event): Promise<Rectangle | null> => {
      if (isWindowOpen) {
        console.warn("[OverlayHandler]: Overlay already open");
        return null;
      }
      isWindowOpen = true;

      try {
        // 1. Get screenshots from .Net (logical coords, physical px screenshots)
        const responses = await getScreenshot(invokeBackend);
        if (!responses || responses.length === 0) {
          console.error("[OverlayHandler]: Cant get the screenshot!");
          return null;
        }

        // 2. Get Electron displays (for scaleFactor — backend cant tell us this)
        const electronDisplays = screen.getAllDisplays();

        // 3. Match backend responses → Electron displays
        const monitorEntries: MonitorEntry[] = matchMonitorsToDisplays(
          responses,
          electronDisplays,
        );

        // 4. Create new window per monitor.
        for (const monitorEntry of monitorEntries) {
          const newWindow = createElectronWindow(isDev, monitorEntry.display);
          monitorEntry.electronWindow = newWindow;
        }

        // 5. Ask .Net to start broadcasting mouse click and drag
        await invokeBackend("System.inputRecordOverlayStart", null);

        // 6. Register per-window ready signal handler BEFORE loading pages
        registerSignalReadyHandlers(monitorEntries);

        // 7. Navigate to overlay page on every window.
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

        // 8. Wait for result (any window can send it — first one wins)
        return await registerSignalCloseHandler(monitorEntries, invokeBackend);
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
function createElectronWindow(isDev: boolean, display: Display): BrowserWindow {
  // Create window
  const newWindow = new BrowserWindow({
    x: display.bounds.x,
    y: display.bounds.y,
    width: display.bounds.width,
    height: display.bounds.height,
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

  // Cover the full display including taskbar area
  // newWindow.setBounds({
  //   x: display.bounds.x,
  //   y: display.bounds.y,
  //   width: display.bounds.width,
  //   height: display.bounds.height,
  // });

  newWindow.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });
  newWindow.setAlwaysOnTop(true, "screen-saver"); // highest possible level
  newWindow.setIgnoreMouseEvents(false);

  //   newWindow.on("show", () => {
  //   newWindow.setAlwaysOnTop(true, "screen-saver", 1);
  //   newWindow.moveTop();
  // });

  return newWindow;
}

//=====================================================================
// Listen for signals from react.
// 'SignalReady' => page loaded and return image from .Net
// 'SignalCloseWindow' => operation completed - return user selection to main electron window
//=====================================================================
function registerSignalReadyHandlers(monitorEntries: MonitorEntry[]): void {
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
          physicalWidth: monitorEntry.screenshotMonitorResponse.physicalWidth,
          physicalHeight: monitorEntry.screenshotMonitorResponse.physicalHeight,
          logicalWidth: monitorEntry.display.bounds.width,
          logicalHeight: monitorEntry.display.bounds.height,
          scaleFactor: monitorEntry.display.scaleFactor,
          monitorLogicalOrigin: {
            x: monitorEntry.display.bounds.x,
            y: monitorEntry.display.bounds.y,
          },
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

    const cleanup = () => {
      invokeBackend("System.inputRecordOverlayStop", null);

      ipcMain.removeHandler(IPC_CHANNELS.OVERLAY_SIGNAL_READY); //remove the READY handler if the user cancelled before signalReady fired
      electronWindows.forEach((window) => {
        if (!window.isDestroyed()) window.close();
      });
    };

    ipcMain.once(
      IPC_CHANNELS.OVERLAY_SIGNAL_CLOSE_WINDOW,
      (_event, rect: Rectangle | null) => {
        cleanup();
        resolve(rect);
      },
    );

    // If user force-closes the overlay window (e.g. Alt+F4)
    electronWindows.forEach((win) => {
      win.once("closed", () => {
        cleanup();
        resolve(null);
      });
    });
  });
}

//=====================================================================
// Monitor Matching
// Backend is DPI-unaware → sees logical coords (same as Electron display.bounds).
// Match by logical origin (x, y). Size may have rounding diff, so don't be strict on w/h.
//=====================================================================

function matchMonitorsToDisplays(
  responses: ScreenshotMonitorResponseDto[],
  displays: Display[],
): MonitorEntry[] {
  const entries: MonitorEntry[] = [];

  for (const response of responses) {
    const x = response.logicalX;
    const y = response.logicalY;
    let display = displays.find((d) => d.bounds.x === x && d.bounds.y === y);

    if (!display) {
      // Fallback: closest by distance (handles 1px rounding drift)
      display = displays.reduce((best, d) => {
        const dist = Math.hypot(d.bounds.x - x, d.bounds.y - y);
        const bestDist = Math.hypot(best.bounds.x - x, best.bounds.y - y);
        return dist < bestDist ? d : best;
      });
      console.warn(
        `[OverlayHandler] No exact match for monitor (${x},${y}), using closest display x=${display.bounds.x} y=${display.bounds.y}`,
      );
    }

    entries.push({
      screenshotMonitorResponse: response,
      display: display,
      electronWindow: null,
    });
  }

  return entries;
}
