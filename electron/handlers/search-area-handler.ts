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

export function registerSearchAreaHandler(
  mainWindow: BrowserWindow | null,
  isDev: boolean,
): void {
  ipcMain.handle(
    IPC_CHANNELS.SEARCH_AREA_WINDOW_OPEN,
    async (): Promise<AreaRect | null> => {
      // ── 1. Virtual screen bounds (multi-monitor + negative coords OK) ──
      const displays = screen.getAllDisplays();
      const minX = Math.min(...displays.map((d) => d.bounds.x));
      const minY = Math.min(...displays.map((d) => d.bounds.y));
      const maxRight = Math.max(
        ...displays.map((d) => d.bounds.x + d.bounds.width),
      );
      const maxBottom = Math.max(
        ...displays.map((d) => d.bounds.y + d.bounds.height),
      );

      const virtualWidth = maxRight - minX;
      const virtualHeight = maxBottom - minY;

     

      // ── 2. Create transparent fullscreen overlay window ───────────────────────
      const overlay = new BrowserWindow({
        x: minX,
        y: minY,
        width: virtualWidth,
        height: virtualHeight,
        fullscreen: false, // We set bounds manually for multi-monitor safety
        frame: true,
        transparent: false,
        // frame: false,
        // transparent: true,
        alwaysOnTop: true,
        skipTaskbar: true,
        resizable: false,
        movable: false,
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

      overlay.setIgnoreMouseEvents(false);
      overlay.setAlwaysOnTop(true, "screen-saver"); // highest possible level

      // ── 3. Load the overlay page ──────────────────────────────────────────────
      if (isDev) {
        await overlay.loadURL("http://localhost:5173/#/search-area-overlay");
      } else {
        await overlay.loadFile(
          path.join(__dirname, "../dist/frontend/index.html"),
          { hash: "/search-area-overlay" },
        );
      }

      overlay.show();
      overlay.focus();

      // ── 4. Wait for overlay renderer to signal it's ready, then send screenshot
      await new Promise<void>((resolve) => {
        ipcMain.once(IPC_CHANNELS.SEARCH_AREA_WINDOW_READY, () => resolve());

        // Safety  — send anyway after 3s if ready signal is missed
        // setTimeout(resolve, 3000);
      });

      // ── 5. Wait for user selection result ─────────────────────────────────────
      return new Promise<AreaRect | null>((resolve) => {
        const cleanup = () => {
          if (!overlay.isDestroyed()) {
            overlay.close();
          }
        };

        ipcMain.once(
          IPC_CHANNELS.SEARCH_AREA_RETURN_RESULT_TO_WINDOW,
          (_event, rect: AreaRect | null) => {
            cleanup();
            resolve(rect);
          },
        );

        // If user force-closes the overlay window (e.g. Alt+F4)
        overlay.once("closed", () => {
          resolve(null);
        });
      });
    },
  );
}
