import { app, BrowserWindow } from "electron";
import type { ChildProcess } from "child_process";
// import { app, session } from 'electron';
// import installExtension, {
//   REACT_DEVELOPER_TOOLS,
// } from "electron-devtools-installer";

import { registerElectronProcessHandler } from "./ipc/handlers/electron-process-handler.js";
import { registerImageEditorHandler } from "./ipc/handlers/image-editor-handler.js";
import { registerBackendRequestHandler } from "./ipc/handlers/backend-request-handler.js";
import { registerBroadcastHandler } from "./ipc/handlers/backend-broadcast-handler.js";

import { BackendService, shouldSpawnBackend } from "./ipc/services/backend-service.js";
import { setBackendQuitting } from "./ipc/services/backend-status-service.js";
import { registerOverlayCaptureHandler } from "./ipc/handlers/overlay-capture-handler.js";
import { registerRecordingBarHandler } from "./ipc/handlers/recording-bar-handler.js";
import { registerFilesHandler } from "./ipc/handlers/files-handler.js";
import { registerAutoUpdaterHandler } from "./ipc/handlers/auto-updater-handler.js";
import { startAutoUpdaterChecks } from "./ipc/services/auto-updater-service.js";
import { IS_DEVELOPING, loadPage, PRELOAD_PATH } from "./app-paths.js";

let mainWindow: BrowserWindow | null = null;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    // Chromium's canvas under the theme's color-scheme, which is the colour behind every page.
    // Electron's default is white, and this shows before the page has painted anything.
    backgroundColor: "#121212",
    webPreferences: {
      preload: PRELOAD_PATH,
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
    },
  });

  loadPage(mainWindow);
  if (IS_DEVELOPING) mainWindow.webContents.openDevTools();

  mainWindow.on("closed", () => {
    mainWindow = null;
  });
}

app.whenReady().then(async () => {
  if (IS_DEVELOPING) {
    // try {
    //   await installExtension(REACT_DEVELOPER_TOOLS, {
    //     loadExtensionOptions: { allowFileAccess: true },
    //   });
    //   console.log("React DevTools installed");
    // } catch (err) {
    //   console.error("Failed to install React DevTools", err);
    // }
  }

  // Before the window, since the page asks for the backend's status and the auto-updater's as it loads.
  registerElectronProcessHandler();
  registerAutoUpdaterHandler();
  startAutoUpdaterChecks();

  createWindow();

  let backendProcess: ChildProcess | null = null;
  if (shouldSpawnBackend()) backendProcess = BackendService().spawnDotNetProcess();

  // ======== Register IPC handlers ==========
  // One failure here used to leave every later handler unregistered while the window was
  // already open, which surfaces as "No handler registered for ..." on the first click.
  try {
    const request = await registerBackendRequestHandler(mainWindow);
    await registerBroadcastHandler(mainWindow);
    await registerOverlayCaptureHandler(mainWindow, request.invokeBackend);
    await registerImageEditorHandler(mainWindow);
    registerRecordingBarHandler(mainWindow);
    registerFilesHandler(mainWindow);
  } catch (err) {
    console.error("[Main]: Failed to register IPC handlers:", err);
  }

  app.on("before-quit", () => {
    setBackendQuitting();
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
