import { app, BrowserWindow, dialog } from "electron";
import { execFile } from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";
// import { app, session } from 'electron';
// import installExtension, {
//   REACT_DEVELOPER_TOOLS,
// } from "electron-devtools-installer";

import { registerBackendHandler } from "./handlers/backend-handler.js";
import { registerSearchAreaHandler } from "./handlers/search-area-handler.js";
import { registerImageEditorHandler } from "./handlers/image-editor-handler.js";

import { BackendService } from "./backend-service.js";

const { autoUpdater } = pkg;

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

let mainWindow: BrowserWindow | null = null;
const isDev = process.env.NODE_ENV === "development" || !app.isPackaged;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
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

  if (isDev) {
    mainWindow.loadURL("http://localhost:5173");
    mainWindow.webContents.openDevTools();
  } else {
    mainWindow.loadFile(path.join(__dirname, "../dist/frontend/index.html"));
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

// Auto-updater dialog
autoUpdater.on("update-downloaded", () => {
  dialog
    .showMessageBox({
      type: "info",
      title: "Update Available",
      message: "A new version is ready. Restart now?",
      buttons: ["Yes", "Later"],
    })
    .then((result) => {
      if (result.response === 0) autoUpdater.quitAndInstall();
    });
});

app.whenReady().then(async () => {
  if (isDev) {
    // try {
    //   await installExtension(REACT_DEVELOPER_TOOLS, {
    //     loadExtensionOptions: { allowFileAccess: true },
    //   });
    //   console.log("React DevTools installed");
    // } catch (err) {
    //   console.error("Failed to install React DevTools", err);
    // }
  } else {
    autoUpdater.checkForUpdatesAndNotify();
  }

  createWindow();

  let backendProcess: ReturnType<typeof execFile> | null = null;

  const ENABLE_DEBUG = true; // true = attach to already-running backend
  if (!ENABLE_DEBUG) {
    backendProcess = BackendService().spawnDotNetProcess(mainWindow, isDev);
  }

  // ======== Register IPC handlers ==========

  await registerBackendHandler(mainWindow, isDev);
  registerSearchAreaHandler(mainWindow, isDev);
  registerImageEditorHandler(mainWindow, isDev);

  app.on("before-quit", () => {
    backendProcess?.kill();
  });

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
