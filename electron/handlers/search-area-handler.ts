import { BrowserWindow, ipcMain, Rectangle, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { IPC_CHANNELS } from "../shared/channels.js";
import { InvokeBackend } from "./backend-handler";
import { MonitorService } from "../monitor-service.js";
import {
  ScreenshotMonitorResponseDto,
  SignalReadyResponse,
  SystemMonitor,
} from "../shared/types.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Make sure user cant open second window.
let isWindowOpen = false;

export function registerSearchAreaHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
  invokeBackend: InvokeBackend,
): void {
  ipcMain.handle(
    IPC_CHANNELS.SEARCH_AREA_WINDOW_OPEN,
    async (
      _event,
      screenshotRequestPayload: any,
    ): Promise<Rectangle | null> => {
      if (isWindowOpen) {
        console.warn(
          "[SearchAreaHandler] Overlay already open — ignoring second call",
        );
        return null;
      }
      isWindowOpen = true;

      try {
        // 1. Get screenshot from .Net.
        // const virtualMonitorssadasdasd = MonitorService().getMonitorsInfo();
        const screenshot = await getScreenshot(
          invokeBackend,
          screenshotRequestPayload,
        );
        if (!screenshot) {
          console.error(
            "[SearchAreaHandler]: Cant get the screenshot from backend!",
          );
          return null;
        }

        // 2. Create window.
        const virtualMonitor = MonitorService().getMonitorsInfo();
        const newWindow = await createWindow(isDev, virtualMonitor);

        // 3. Listen for 'SignalReady' from react.
        // handleSignalReady(screenshot, virtualMonitor);

        // 4. Redirect window to the window page
        if (isDev) {
          await newWindow.loadURL(
            "http://localhost:5173/#/search-area-overlay",
          );
        } else {
          await newWindow.loadFile(
            path.join(__dirname, "../dist/frontend/index.html"),
            { hash: "/search-area-overlay" },
          );
        }

        // 5. Wait for user selection and return value to caller of "open window"
        return await handleUserSelection(newWindow);
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
  screenshotRequestPayload: any,
): Promise<ScreenshotMonitorResponseDto[] | null> {
  try {
    const result = await invokeBackend("System.captureForOverlay", null);

    return (
      (result as { success: boolean; data: ScreenshotMonitorResponseDto[] })
        .data ?? null
    );
  } catch (err) {
    console.error("[SearchAreaHandler] Screenshot failed:", err);
    return null;
  }
}

//=====================================================================
// Create and open window
//=====================================================================
async function createWindow(
  isDev: boolean,
  virtualMonitor: SystemMonitor,
): Promise<BrowserWindow> {
  // const logicalRect = MonitorService().getLogicalVirtualScreen();

  // Create window
  const newWindow = new BrowserWindow({
    x: virtualMonitor.minVirtualX,
    y: virtualMonitor.minVirtualY,
    width: virtualMonitor.logicalVirtualWidth,
    height: virtualMonitor.logicalVirtualHeight,
    fullscreen: false, // We set bounds manually for multi-monitor safety
    frame: true,
    transparent: false,
    // frame: false,
    // transparent: true,
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    // movable: false,
    movable: true,
    focusable: true,
    hasShadow: false,
    webPreferences: {
      preload: path.join(
        __dirname,
        isDev ? "../preload.js" : "../dist/preload.js",
      ),
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
    },
  });

  // Set bounds again... why the fuck initial values doesnt "apply"???
  // newWindow.setBounds({
  //   x: virtualMonitor.minVirtualX,
  //   y: virtualMonitor.minVirtualY,
  //   width: virtualMonitor.physicalVirtualWidth,
  //   height: virtualMonitor.physicalVirtualHeight,
  // });
  newWindow.setBounds({
    x: virtualMonitor.minVirtualX,
    y: virtualMonitor.minVirtualY,
    width: virtualMonitor.logicalVirtualWidth,
    height: virtualMonitor.logicalVirtualHeight,
  });
  newWindow.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true });
  newWindow.setIgnoreMouseEvents(false);
  newWindow.setAlwaysOnTop(true, "screen-saver"); // highest possible level

  return newWindow;

  // overlay.show();
  // overlay.focus();
}

//=====================================================================
// Listen for 'SignalReady' from react, and return image from .Net
//=====================================================================
function handleSignalReady(
  screenshot: Uint8Array,
  virtualMonitor: SystemMonitor,
): void {
  ipcMain.handle(
    IPC_CHANNELS.SEARCH_AREA_WINDOW_READY,
    async (): Promise<SignalReadyResponse> => {
      ipcMain.removeHandler(IPC_CHANNELS.SEARCH_AREA_WINDOW_READY); // Self-remove first - one invocation per window lifecycle

      return {
        screenshot: screenshot,
        monitorsInfo: virtualMonitor.displays,
        physicalWidth: virtualMonitor.physicalVirtualWidth,
        physicalHeight: virtualMonitor.physicalVirtualHeight,
      };
    },
  );
}

//=====================================================================
// Wait for user selection from react
//=====================================================================

function handleUserSelection(
  newWindow: BrowserWindow,
): Promise<Rectangle | null> {
  return new Promise<Rectangle | null>((resolve) => {
    const cleanup = () => {
      ipcMain.removeHandler(IPC_CHANNELS.SEARCH_AREA_WINDOW_READY); //remove the READY handler if the user cancelled before signalReady fired
      if (!newWindow.isDestroyed()) newWindow.close();
    };

    ipcMain.once(
      IPC_CHANNELS.SEARCH_AREA_RETURN_RESULT_TO_WINDOW,
      (_event, rect: Rectangle | null) => {
        cleanup();
        resolve(rect);
      },
    );

    // If user force-closes the overlay window (e.g. Alt+F4)
    newWindow.once("closed", () => resolve(null));
  });
}
