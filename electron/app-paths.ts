import { app, type BrowserWindow } from "electron";
import path from "path";
import { fileURLToPath } from "url";

// Where the compiled main sits. Dev and a release compile to the same dist/electron, with the
// frontend's build beside it in dist/frontend, so every path below is one path.
export const OUT_DIR = path.dirname(fileURLToPath(import.meta.url));

export const PRELOAD_PATH = path.join(OUT_DIR, "preload.js");

/** Vite serves the pages while developing; a release has them built. */
export const IS_DEVELOPING = !app.isPackaged;

const DEV_SERVER_URL = process.env.VITE_DEV_SERVER_URL ?? "http://localhost:5173";

/** A page of the frontend by its route, such as "/image-editor". */
export function loadPage(window: BrowserWindow, route: string = "/"): Promise<void> {
  if (IS_DEVELOPING) return window.loadURL(`${DEV_SERVER_URL}/#${route}`);

  return window.loadFile(path.join(OUT_DIR, "../frontend/index.html"), { hash: route });
}
