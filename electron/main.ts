import { app, BrowserWindow, dialog, ipcMain } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import pkg from "electron-updater";
import { ChildProcess, execFile, spawn } from "child_process";

const { autoUpdater } = pkg;
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

let backend: ChildProcess | null = null;
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
    // mainWindow.loadFile(path.join(__dirname, "frontend/index.html"));
    mainWindow.loadFile(path.join(__dirname, "../dist/frontend/index.html"));
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on("closed", () => {});
}

// show dialog on update
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
  // autoUpdater.checkForUpdatesAndNotify();
  if (!isDev) autoUpdater.checkForUpdatesAndNotify(); // Skip in dev to avoid errors
  createWindow();

  // Backend spawn
  const backendPath = isDev
    ? "dotnet"
    : path.join(process.resourcesPath, "backend/App.exe");
  const args = isDev
    ? ["run", "--project", path.join(__dirname, "../backend/App/App.csproj")]
    : [];

  backend = execFile(backendPath, (error, stdout, stderr) => {
    if (error) {
      console.error("Failed to run .exe:", error);
      return;
    }
    console.log(" .exe output:", stdout);
  });

  // IPC from React to backend
  backend.stdout?.on("data", (data) => {
    data
      .toString()
      .trim()
      .split("\n")
      .forEach((line: any) => {
        try {
          const msg = JSON.parse(line);
          mainWindow?.webContents.send("backend-message", msg); // Push to React
        } catch {}
      });
  });

  backend.stderr?.on("data", (data) =>
    console.error("Backend error:", data.toString()),
  );
  backend.on("close", () => console.log("Backend closed"));

  ipcMain.handle("send-to-backend", (_, msg: object) => {
    backend?.stdin?.write(JSON.stringify(msg) + "\n");
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
