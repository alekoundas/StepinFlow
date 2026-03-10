import { app, BrowserWindow, dialog, ipcMain } from "electron";
import { execFile } from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";
import net from "net";
import log from "electron-log";
import { IpcHandlerService } from "./IpcHandlerService.js";
import { ProtobufService } from "./protobuf/protobuf.js";

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

app.whenReady().then(async () => {
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev
  // const ENABLE_DEBUG = false;
  const ENABLE_DEBUG = true;
  createWindow();

  let backendClient: net.Socket | null = null; // For named pipe connection
  let backendProcess: ReturnType<typeof execFile> | null = null;

  if (ENABLE_DEBUG) {
    backendClient = await IpcHandlerService().connectToDotNetPipe(mainWindow);
  } else {
    backendProcess = IpcHandlerService().spawnDotNetProcess(mainWindow, isDev);
    // Give backend time to create the pipe server
    setTimeout(async () => {
      if (!backendProcess?.killed) {
        backendClient =
          await IpcHandlerService().connectToDotNetPipe(mainWindow);
      }
    }, 1200);
  }

  // TODO REMOVE FROM HERE
  const { IpcRequest } = await ProtobufService().getMessageTypes();
  ipcMain.handle("send-to-backend", async (_, msg: RequestMessage) => {
    // console.log("[Electron]Sent to backend:", msg);

    // if (backendClient) {
    //   backendClient.write(JSON.stringify(msg) + "\n");
    // } else {
    //   log.error("[Electron]Backend process not available for sending message");
    //   console.error(
    //     "[Electron] Backend process not available for sending message",
    //   );
    // }

    const payloadBytes = Buffer.isBuffer(msg.payload)
      ? msg.payload
      : Buffer.from(JSON.stringify(msg.payload)); // fallback for small objects

    const reqObj = {
      action: msg.action,
      payload: payloadBytes,
      correlationId: msg.correlationId ?? crypto.randomUUID(),
    };

    const err = IpcRequest.verify(reqObj);
    if (err) throw Error(err);

    const message = IpcRequest.create(reqObj);
    const buffer = IpcRequest.encode(message).finish();

    const prefix = Buffer.alloc(4);
    prefix.writeUInt32BE(buffer.length, 0);

    // backendClient?.write(Buffer.concat([prefix, buffer]));

    if (!backendClient || !backendClient.writable) {
      log.log("[Electron]Backend pipe not connected");
      console.log("[Electron]Backend pipe not connected");
      throw new Error("[Electron]Backend pipe not connected");
    }
    backendClient.write(Buffer.concat([prefix, buffer]));
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
