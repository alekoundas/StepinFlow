import { app, BrowserWindow, dialog, ipcMain } from "electron";
import { execFile } from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";
import net from "net";
import log from "electron-log";
import { IpcHandlerService } from "./IpcHandlerService.js";

interface RequestMessage {
  action: string;
  payload: unknown; // TODO use a  type (intersection type?)
  correlationId?: string; // Optional ID to match requests with responses
}

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

  mainWindow.on("closed", () => {});
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

app.whenReady().then(() => {
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev
  const ENABLE_DEBUG = false;
  // const ENABLE_DEBUG = true;
  createWindow();

  let backendClient: net.Socket | null = null; // For named pipe connection
  let backendProcess: ReturnType<typeof execFile> | null = null;

  if (ENABLE_DEBUG) {
    backendClient = IpcHandlerService().connectToDotNetPipe(mainWindow);
  } else {
    backendProcess = IpcHandlerService().spawnDotNetProcess(mainWindow, isDev);
    // Give backend time to create the pipe server
    setTimeout(() => {
      if (!backendProcess?.killed) {
        backendClient = IpcHandlerService().connectToDotNetPipe(mainWindow);
      }
    }, 1200);
  }

  ipcMain.handle("send-to-backend", (_, msg: RequestMessage) => {
    console.log("[Electron]Sent to backend:", msg);

    if (backendClient) {
      backendClient.write(JSON.stringify(msg) + "\n");
    } else {
      log.error("[Electron]Backend process not available for sending message");
      console.error(
        "[Electron] Backend process not available for sending message",
      );
    }
  });

  app.on("before-quit", () => {
    backendClient?.destroy();
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
