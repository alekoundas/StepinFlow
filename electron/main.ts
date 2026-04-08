import { app, BrowserWindow, dialog, ipcMain } from "electron";
import { execFile } from "child_process";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import path from "path";
import net from "net";
import log from "electron-log";
import { IpcHandlerService } from "./IpcHandlerService.js";
import { ProtobufService } from "./protobuf/protobuf.js";
import { registerBackendHandler } from "./handlers/backend-handler.js";
import { registerSearchAreaHandler } from "./handlers/search-area-handler.js";

// interface RequestMessage {
//   action: string;
//   payload: unknown; // TODO use a  type (intersection type?)
//   correlationId?: string; // Optional ID to match requests with responses
// }

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
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev

  createWindow();

  let backendClient: net.Socket | null = null;
  let backendProcess: ReturnType<typeof execFile> | null = null;
  const pendingResponses = new Map<string, (plain: any) => void>();

  const getBackendClient = () => backendClient;
  const setBackendClient = (client: net.Socket | null) => {
    backendClient = client;
  };

  // const ENABLE_DEBUG = false;
  const ENABLE_DEBUG = true;
  if (ENABLE_DEBUG) {
    backendClient = await IpcHandlerService().connectToDotNetPipe(
      mainWindow,
      setBackendClient,
      pendingResponses,
    );
  } else {
    backendProcess = IpcHandlerService().spawnDotNetProcess(mainWindow, isDev);
    backendClient = await IpcHandlerService().connectToDotNetPipe(
      mainWindow,
      setBackendClient,
      pendingResponses,
    );
  }

  await registerBackendHandler(
    getBackendClient,
    setBackendClient,
    pendingResponses,
    mainWindow,
  );

  registerSearchAreaHandler(mainWindow, isDev);

  // TODO REMOVE FROM HERE
  // const { IpcRequest } = await ProtobufService().getMessageTypes();
  // ipcMain.handle(IPC_CHANNELS.BACKEND_SEND, async (_, msg: RequestMessage) => {
  //   console.log("[Electron]Sent to backend:", msg);

  //   if (!backendClient || !backendClient.writable) {
  //     log.log("[Electron] Backend pipe not connected, attempting reconnect...");
  //     console.log(
  //       "[Electron] Backend pipe not connected, attempting reconnect...",
  //     );
  //     try {
  //       backendClient = await IpcHandlerService().connectToDotNetPipe(
  //         mainWindow,
  //         setBackendClient,
  //         pendingResponses,
  //       );
  //     } catch (reconnectErr) {
  //       log.error("[Electron] Reconnect failed:", reconnectErr);
  //       throw new Error("[Electron] Backend pipe not available");
  //     }
  //   }

  //   const payloadBytes =
  //     // Buffer.isBuffer(msg.payload)
  //     //   ? msg.payload
  //     //   :
  //     Buffer.from(JSON.stringify(msg.payload)); // fallback for small objects

  //   const reqObj: RequestMessage = {
  //     action: msg.action,
  //     payload: payloadBytes,
  //     correlationId: crypto.randomUUID(),
  //   };

  //   const err = IpcRequest.verify(reqObj);
  //   if (err) throw Error(err);

  //   const message = IpcRequest.create(reqObj);
  //   const buffer = IpcRequest.encode(message).finish();

  //   const prefix = Buffer.alloc(4);
  //   prefix.writeUInt32BE(buffer.length, 0);

  //   backendClient.write(Buffer.concat([prefix, buffer]));
  //   return new Promise((resolve, reject) => {
  //     const cid = reqObj.correlationId!;
  //     const timeoutId = setTimeout(() => {
  //       if (pendingResponses.has(cid)) {
  //         pendingResponses.delete(cid);
  //         reject(
  //           new Error(
  //             `Timeout waiting for backend response to "${msg.action}"`,
  //           ),
  //         );
  //       }
  //     }, 30000);

  //     pendingResponses.set(cid, (plain: any) => {
  //       clearTimeout(timeoutId);
  //       if (plain.error) {
  //         reject(new Error(plain.error));
  //         return;
  //       }

  //       // Auto-parse JSON payload (matches your frontend convention)
  //       let payload = plain.payload;
  //       if (Buffer.isBuffer(payload)) {
  //         try {
  //           payload = JSON.parse(payload.toString("utf-8"));
  //         } catch {
  //           // keep raw Buffer if not JSON
  //         }
  //       }
  //       resolve(payload);
  //     });
  //   });
  // });

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
