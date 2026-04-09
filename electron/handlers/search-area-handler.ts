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
    IPC_CHANNELS.SEARCH_AREA_OPEN,
    async (): Promise<AreaRect | null> => {
      // ── 1. Capture screenshot BEFORE the overlay opens ──────────────────────
      const primaryDisplay = screen.getPrimaryDisplay();
      const { width, height } = primaryDisplay.size;

      const sources = await desktopCapturer.getSources({
        types: ["screen"],
        thumbnailSize: { width, height },
      });

      // Pick the source that matches the primary display (fallback to first)
      const primarySource =
        sources.find((s) =>
          s.display_id
            ? s.display_id === String(primaryDisplay.id)
            : s.name.includes("Entire Screen") || s.name.includes("Screen 1"),
        ) ?? sources[0];

      if (!primarySource) {
        throw new Error("[SearchArea] No screen source found for capture");
      }

      const screenshotDataUrl = primarySource.thumbnail.toDataURL();

      // ── 2. Create transparent fullscreen overlay window ───────────────────────
      const overlay = new BrowserWindow({
        width,
        height,
        x: primaryDisplay.bounds.x,
        y: primaryDisplay.bounds.y,
        fullscreen: false, // We set bounds manually for multi-monitor safety
        frame: false,
        transparent: true,
        alwaysOnTop: true,
        skipTaskbar: true,
        resizable: false,
        movable: false,
        focusable: true,
        hasShadow: false,
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

      overlay.setIgnoreMouseEvents(false);
      overlay.setAlwaysOnTop(true, "screen-saver"); // highest possible level

      // ── 3. Load the overlay page ──────────────────────────────────────────────
      if (isDev) {
        await overlay.loadURL("http://localhost:5173/search-area-overlay");
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
        ipcMain.once(IPC_CHANNELS.SEARCH_AREA_READY, () => resolve());

        // Safety timeout — send anyway after 3s if ready signal is missed
        setTimeout(resolve, 3000);
      });

      overlay.webContents.send(
        IPC_CHANNELS.SEARCH_AREA_SCREENSHOT,
        screenshotDataUrl,
      );

      // ── 5. Wait for user selection result ─────────────────────────────────────
      return new Promise<AreaRect | null>((resolve) => {
        const cleanup = () => {
          if (!overlay.isDestroyed()) {
            overlay.close();
          }
        };

        ipcMain.once(
          IPC_CHANNELS.SEARCH_AREA_RESULT,
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
